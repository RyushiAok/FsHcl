import {
  Cross1Icon,
  GitHubLogoIcon,
  HamburgerMenuIcon,
} from "@radix-ui/react-icons";
import { useEffect, useState } from "react";
import { Link, useLocation } from "react-router";
import { NavTree } from "~/ui/nav";
import {
  backdrop,
  drawer,
  headerInner,
  headerRight,
  headerStyle,
  iconLink,
  logoLink,
  logoTitle,
  menuButton,
} from "./header-style.css";

const drawerId = "mobile-navigation";

export const Header = () => {
  const [open, setOpen] = useState(false);
  const location = useLocation();

  useEffect(() => {
    setOpen(false);
  }, [location.pathname]);

  useEffect(() => {
    document.body.style.overflow = open ? "hidden" : "";

    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") setOpen(false);
    };
    if (open) document.addEventListener("keydown", closeOnEscape);

    return () => {
      document.body.style.overflow = "";
      document.removeEventListener("keydown", closeOnEscape);
    };
  }, [open]);

  return (
    <>
      <header className={headerStyle}>
        <div className={headerInner}>
          <Link to="/" className={logoLink} viewTransition>
            <span className={logoTitle}>FsHcl</span>
          </Link>
          <div className={headerRight}>
            <a
              href="https://github.com/RyushiAok/FsHcl"
              target="_blank"
              rel="noopener noreferrer"
              className={iconLink}
              aria-label="FsHcl on GitHub"
            >
              <GitHubLogoIcon width="1.5em" height="1.5em" />
            </a>
            <button
              className={menuButton}
              type="button"
              onClick={() => setOpen((value) => !value)}
              aria-label={open ? "Close menu" : "Open menu"}
              aria-controls={drawerId}
              aria-expanded={open}
            >
              {open ? (
                <Cross1Icon width="1.25em" height="1.25em" />
              ) : (
                <HamburgerMenuIcon width="1.25em" height="1.25em" />
              )}
            </button>
          </div>
        </div>
      </header>

      <div
        className={backdrop[open ? "open" : "closed"]}
        onClick={() => setOpen(false)}
        aria-hidden="true"
      />
      <nav
        id={drawerId}
        className={drawer[open ? "open" : "closed"]}
        aria-label="Documentation"
        aria-hidden={!open}
        inert={!open}
      >
        <NavTree pathname={location.pathname} variant="drawer" />
      </nav>
    </>
  );
};
