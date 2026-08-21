"use client";

import { createContext, useContext, useEffect, useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import * as authApi from "@/lib/api/auth";
import * as cartApi from "@/lib/api/cart";
import { clearGuestCartId, getGuestCartId } from "@/lib/cart/guestCartId";
import { decodeJwtPayload } from "@/lib/utils/decodeJwt";
import { Session } from "@/types/auth";

interface AuthState {
  accessToken: string | null;
  /** Also CartService's cart id for this user — its cart id convention is `Id == UserId`. */
  userId: string | null;
  email: string | null;
}

interface AuthContextValue extends AuthState {
  /** True until the mount-time silent-refresh check resolves — avoids a "logged out" flash. */
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, phoneNumber?: string) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

// Refresh a minute before the access token actually expires, so an in-flight request never
// races an expiring token. Never schedule sooner than 5s out (a clock skew / near-expiry edge
// case shouldn't cause a refresh storm).
const REFRESH_SKEW_MS = 60_000;
const MIN_REFRESH_DELAY_MS = 5_000;

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [state, setState] = useState<AuthState>({ accessToken: null, userId: null, email: null });
  const [isLoading, setIsLoading] = useState(true);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const refreshInFlightRef = useRef<Promise<Session | null> | null>(null);
  const queryClient = useQueryClient();

  // /api/auth/refresh ROTATES the refresh token server-side (the old one is revoked the moment
  // it's used), so two refresh() calls that overlap in flight are unsafe: whichever reaches the
  // backend second is presenting an already-revoked token and gets a 401, even though the first
  // one succeeded — React StrictMode's dev-only double-invoke of the mount effect below hits
  // this exact race and was observed logging a freshly-restored session straight back out.
  // Sharing one in-flight promise across overlapping callers fixes it for any concurrent caller,
  // not just StrictMode's.
  function dedupedRefresh(): Promise<Session | null> {
    if (!refreshInFlightRef.current) {
      refreshInFlightRef.current = authApi.refresh().finally(() => {
        refreshInFlightRef.current = null;
      });
    }
    return refreshInFlightRef.current;
  }

  function clearRefreshTimer() {
    if (timerRef.current) {
      clearTimeout(timerRef.current);
      timerRef.current = null;
    }
  }

  function clearSession() {
    setState({ accessToken: null, userId: null, email: null });
    clearRefreshTimer();
  }

  function applySession(session: Session) {
    const claims = decodeJwtPayload(session.accessToken);
    setState({
      accessToken: session.accessToken,
      userId: claims?.sub ?? null,
      email: claims?.email ?? null,
    });

    clearRefreshTimer();
    const delay = Math.max(
      new Date(session.accessTokenExpiresAt).getTime() - Date.now() - REFRESH_SKEW_MS,
      MIN_REFRESH_DELAY_MS,
    );
    timerRef.current = setTimeout(async () => {
      const refreshed = await dedupedRefresh();
      if (refreshed) applySession(refreshed);
      else clearSession();
    }, delay);
  }

  useEffect(() => {
    let cancelled = false;
    dedupedRefresh()
      .then((session) => {
        if (cancelled) return;
        if (session) applySession(session);
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });
    return () => {
      cancelled = true;
      clearRefreshTimer();
    };
    // Intentionally run once on mount only — applySession/clearRefreshTimer are stable enough
    // in practice (recreated per render but only invoked here and from event handlers below).
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function mergeGuestCartIfAny(accessToken: string) {
    const guestCartId = getGuestCartId();
    if (!guestCartId) return;
    try {
      await cartApi.mergeCart(accessToken, guestCartId);
    } finally {
      clearGuestCartId();
      await queryClient.invalidateQueries({ queryKey: ["cart"] });
    }
  }

  async function login(email: string, password: string) {
    const session = await authApi.login(email, password);
    applySession(session);
    await mergeGuestCartIfAny(session.accessToken);
  }

  async function register(email: string, password: string, phoneNumber?: string) {
    const session = await authApi.register(email, password, phoneNumber);
    applySession(session);
    await mergeGuestCartIfAny(session.accessToken);
  }

  async function logout() {
    await authApi.logout();
    clearSession();
    await queryClient.invalidateQueries({ queryKey: ["cart"] });
  }

  const value: AuthContextValue = { ...state, isLoading, login, register, logout };
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
