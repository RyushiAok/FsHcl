import { NavLink } from "react-router";
import * as s from "./sidebar-style.css";

interface NavItem {
  to: string;
  label: string;
}

interface NavSection {
  title: string | null;
  items: NavItem[];
}

export const navSections: NavSection[] = [
  {
    title: null,
    items: [
      { to: "/getting-started", label: "Getting Started" },
      { to: "/values", label: "Values" },
      { to: "/syntax", label: "Syntax" },
      { to: "/terraform", label: "Terraform Helpers" },
      { to: "/patterns", label: "Patterns" },
    ],
  },
  {
    title: "API Reference",
    items: [
      { to: "/api-types", label: "Types" },
      { to: "/api-values", label: "Values" },
      { to: "/api-syntax", label: "Syntax" },
      { to: "/api-render", label: "Render" },
      { to: "/api-terraform", label: "TerraformHcl" },
    ],
  },
];

export const navItems = navSections.flatMap((section) => section.items);

export const Sidebar = () => (
  <nav className={s.nav}>
    {navSections.map((section, i) => (
      <div key={i}>
        {section.title && (
          <div className={s.sectionHeading}>{section.title}</div>
        )}
        <ul className={s.list}>
          {section.items.map(({ to, label }) => (
            <li key={to}>
              <NavLink
                to={to}
                viewTransition
                className={({ isActive }) =>
                  isActive ? s.linkActive : s.link
                }
              >
                {label}
              </NavLink>
            </li>
          ))}
        </ul>
      </div>
    ))}
  </nav>
);
