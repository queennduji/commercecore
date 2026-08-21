import { env } from "@/lib/env";

/**
 * Every backend service in this platform returns `{ errors: string[] }` on failure - both its
 * `ServiceResult<T>.Failure(...)` business-rule path (`BadRequest(new { errors = result.Errors })`,
 * `result.Errors` being an `IReadOnlyCollection<string>`) and its FluentValidation
 * `IExceptionHandler` path (`errors = validationException.Errors.Select(e => e.ErrorMessage)`)
 * serialize the same flat-array shape - verified against both, not just one, since ASP.NET's own
 * default `ValidationProblemDetails` would instead nest errors per field name and this codebase
 * deliberately doesn't use that default.
 */
export interface ProblemDetails {
  errors?: string[];
  [key: string]: unknown;
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem: ProblemDetails | undefined,
    path: string,
  ) {
    super(problem?.errors?.[0] ?? `Request to ${path} failed with status ${status}`);
    this.name = "ApiError";
  }
}

type ApiFetchInit = RequestInit & {
  /** Next.js fetch cache/revalidation options - passed straight through. */
  next?: { revalidate?: number | false; tags?: string[] };
};

/**
 * Thin typed wrapper around `fetch` for calls to the ApiGateway. Deliberately plain
 * (isomorphic) fetch with no "use server" - Phase 1 only ever calls it from Server
 * Components, but it's written to also work unchanged from Client Components once
 * CORS is enabled on the gateway (see Phase 2 in the root plan).
 */
export async function apiFetch<T>(path: string, init?: ApiFetchInit): Promise<T> {
  const url = `${env.apiBaseUrl}${path}`;

  let response: Response;
  try {
    response = await fetch(url, {
      ...init,
      headers: {
        Accept: "application/json",
        ...(init?.body ? { "Content-Type": "application/json" } : {}),
        ...init?.headers,
      },
    });
  } catch (cause) {
    throw new Error(`Could not reach the API gateway at ${url}. Is it running?`, { cause });
  }

  if (!response.ok) {
    let problem: ProblemDetails | undefined;
    try {
      problem = await response.json();
    } catch {
      // body wasn't JSON (or was empty) - ApiError falls back to a generic message
    }
    throw new ApiError(response.status, problem, path);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

/** Builds a query string, skipping undefined/null values. */
export function toQueryString(params: Record<string, string | number | boolean | undefined | null>): string {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null) {
      search.set(key, String(value));
    }
  }
  const qs = search.toString();
  return qs ? `?${qs}` : "";
}
