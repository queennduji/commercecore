"use client";

import Link from "next/link";
import { toast } from "sonner";
import { useAuth } from "@/components/auth/AuthProvider";
import { Button } from "@/components/ui/button";

export function AccountMenu() {
  const { email, isLoading, logout } = useAuth();

  async function handleLogout() {
    await logout();
    toast.success("Logged out");
  }

  if (isLoading) {
    return <div className="h-8 w-16" />;
  }

  if (email) {
    return (
      <div className="flex items-center gap-3 text-sm">
        <span className="hidden text-muted-foreground sm:inline">{email}</span>
        <Link href="/orders" className="font-medium transition-colors hover:text-muted-foreground">
          My Orders
        </Link>
        <Button variant="ghost" size="sm" onClick={handleLogout}>
          Log out
        </Button>
      </div>
    );
  }

  return (
    <Link href="/login" className="text-sm font-medium transition-colors hover:text-muted-foreground">
      Log in
    </Link>
  );
}
