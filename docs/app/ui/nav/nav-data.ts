export interface NavItem {
  to: string;
  label: string;
}

export interface NavSubGroup {
  title: string;
  items: readonly NavItem[];
}

export type NavGroupEntry = NavItem | NavSubGroup;

export type NavEntryChunk =
  | { kind: "items"; items: readonly NavItem[] }
  | { kind: "subgroup"; subgroup: NavSubGroup };

export interface NavGroup {
  title: string;
  entries: readonly NavGroupEntry[];
}

export interface NavSection {
  title: string | null;
  items?: readonly NavItem[];
  groups?: readonly NavGroup[];
}

export function isNavItem(entry: NavGroupEntry): entry is NavItem {
  return "to" in entry;
}

export function collectNavItems(entries: readonly NavGroupEntry[]): NavItem[] {
  return entries.flatMap((entry) =>
    isNavItem(entry) ? [entry] : entry.items,
  );
}

export function chunkNavEntries(
  entries: readonly NavGroupEntry[],
): NavEntryChunk[] {
  return entries.reduce<NavEntryChunk[]>((chunks, entry) => {
    if (!isNavItem(entry)) {
      return [...chunks, { kind: "subgroup", subgroup: entry }];
    }

    const previous = chunks.at(-1);
    if (previous?.kind !== "items") {
      return [...chunks, { kind: "items", items: [entry] }];
    }

    return [
      ...chunks.slice(0, -1),
      { kind: "items", items: [...previous.items, entry] },
    ];
  }, []);
}

export function hasActiveNavItem(
  items: readonly NavItem[],
  pathname: string,
) {
  return items.some(({ to }) => to === pathname);
}

export function hasActiveNavEntry(
  entries: readonly NavGroupEntry[],
  pathname: string,
) {
  return hasActiveNavItem(collectNavItems(entries), pathname);
}

export const navSections: readonly NavSection[] = [
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
    groups: [
      {
        title: "FsHcl",
        entries: [
          { to: "/api-types", label: "Types" },
          { to: "/api-values", label: "Values" },
          { to: "/api-syntax", label: "Syntax" },
          { to: "/api-render", label: "Render" },
          { to: "/api-terraform", label: "TerraformHcl" },
        ],
      },
    ],
  },
];

export const navItems = navSections.flatMap((section) => [
  ...(section.items ?? []),
  ...(section.groups?.flatMap((group) => collectNavItems(group.entries)) ?? []),
]);

export function adjacentNavItems(pathname: string) {
  const currentIndex = navItems.findIndex(({ to }) => to === pathname);
  if (currentIndex < 0) {
    return { previous: null, next: null };
  }

  return {
    previous: currentIndex > 0 ? navItems[currentIndex - 1] : null,
    next: navItems.at(currentIndex + 1) ?? null,
  };
}
