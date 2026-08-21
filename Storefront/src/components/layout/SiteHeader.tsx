import Link from "next/link";
import { AccountMenu } from "@/components/layout/AccountMenu";
import { HeaderCartBadge } from "@/components/layout/HeaderCartBadge";

export function SiteHeader() {
  return (
    <header className="border-b border-border bg-background">
      <div className="mx-auto flex h-16 max-w-6xl items-center justify-between gap-2 px-4 sm:px-6">
        <Link href="/" className="shrink-0 text-lg font-semibold tracking-tight">
          CommerceCore
        </Link>
        {/* "Home" is dropped below sm - the logo already links there, and at a 375px width
            (iPhone SE and similar) fitting logo + both nav links + cart + account genuinely
            overflows the header (confirmed directly: content wants ~16px more than available).
            "Products" stays at every size since it's not reachable any other way from here. */}
        <nav className="flex items-center gap-3 text-sm font-medium text-muted-foreground sm:gap-6">
          <Link href="/" className="hidden transition-colors hover:text-foreground sm:inline">
            Home
          </Link>
          <Link href="/products" className="transition-colors hover:text-foreground">
            Products
          </Link>
        </nav>
        <div className="flex items-center gap-3 sm:gap-5">
          <HeaderCartBadge />
          <AccountMenu />
        </div>
      </div>
    </header>
  );
}
