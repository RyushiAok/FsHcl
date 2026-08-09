import { mkdirSync } from "node:fs";
import { readFileSync, writeFileSync, readdirSync } from "node:fs";
import { join } from "node:path";

const root = join(import.meta.dirname, "..");
const refDir = join(root, ".fsdocs-out", "reference");
const outDir = join(root, "docs", "app", "content");

type Source = {
  html: string;
  heading: string;
};

type FilePage = {
  subdir: string;
  namespace: string;
  slug: string;
  title: string;
  description: string;
  sources: Source[];
};

type Parameter = {
  name: string;
  type: string;
};

type Member = {
  name: string;
  signature: string;
  summary: string;
  params: Parameter[];
  returns: string;
};

type MemberSection = {
  section: string;
  members: Member[];
};

type SourceSection = {
  title: string;
  members: Member[];
};

// ── File-based page definitions ──────────────────────────────────────

const filePages: FilePage[] = [
  // ── FsHcl.Hcl ──
  {
    subdir: "fshcl",
    namespace: "FsHcl.Hcl",
    slug: "api-types",
    title: "Types",
    description: "Core HCL types: Value, Node, Expr, RenderOptions.",
    sources: [
      { html: "fshcl-hcl-value.html", heading: "Value" },
      { html: "fshcl-hcl-node.html", heading: "Node" },
      { html: "fshcl-hcl-expr.html", heading: "Expr" },
      { html: "fshcl-hcl-renderoptions.html", heading: "RenderOptions" },
    ],
  },
  {
    subdir: "fshcl",
    namespace: "FsHcl.Hcl",
    slug: "api-values",
    title: "Values Module",
    description: "Value constructors and conversion functions.",
    sources: [{ html: "fshcl-hcl-values.html", heading: "Values" }],
  },
  {
    subdir: "fshcl",
    namespace: "FsHcl.Hcl",
    slug: "api-syntax",
    title: "Syntax Module",
    description: "HCL block and attribute builder functions.",
    sources: [{ html: "fshcl-hcl-syntax.html", heading: "Syntax" }],
  },
  {
    subdir: "fshcl",
    namespace: "FsHcl.Hcl",
    slug: "api-render",
    title: "Render Module",
    description: "HCL rendering functions.",
    sources: [{ html: "fshcl-hcl-render.html", heading: "Render" }],
  },
  // ── FsHcl.TerraformHcl ──
  {
    subdir: "fshcl",
    namespace: "FsHcl",
    slug: "api-terraform",
    title: "TerraformHcl Module",
    description: "Terraform-specific block and attribute helpers.",
    sources: [{ html: "fshcl-terraformhcl.html", heading: "TerraformHcl" }],
  },
];

// ── HTML helpers ──────────────────────────────────────────────────────

function decodeEntities(text: string): string {
  return text
    .replace(/&#32;/g, " ")
    .replace(/&#39;/g, "'")
    .replace(/&amp;/g, "&")
    .replace(/&lt;/g, "<")
    .replace(/&gt;/g, ">")
    .replace(/&quot;/g, '"');
}

function stripTags(html: string): string {
  return decodeEntities(html.replace(/<[^>]+>/g, "")).trim();
}

function stripTagsKeepCode(html: string): string {
  return decodeEntities(
    html
      .replace(/<(?:code|c)>([\s\S]*?)<\/(?:code|c)>/g, "`$1`")
      .replace(/<[^>]+>/g, ""),
  ).trim();
}

function escapeMdx(text: string): string {
  return text.replace(/(\w+)<([^>]+)>/g, "`$1<$2>`");
}

function escapeTableCell(text: string): string {
  return text.replace(/(?<!\\)\|/g, "\\|");
}

function extractMembers(html: string): Member[] {
  const members: Member[] = [];
  const tableRegex =
    /<tr>\s*<td class="fsdocs-member-usage">([\s\S]*?)<\/td>\s*<td class="fsdocs-member-xmldoc">([\s\S]*?)<\/td>\s*<\/tr>/g;

  let match: RegExpExecArray | null;
  while ((match = tableRegex.exec(html)) !== null) {
    const usageHtml = match[1];
    const docHtml = match[2];

    const idMatch = usageHtml.match(/<a id="([^"]+)">/);
    const name = idMatch ? idMatch[1] : "";

    const sigMatch = usageHtml.match(
      /<a href="#[^"]*">\s*<code>([\s\S]*?)<\/code>\s*<\/a>/,
    );
    const signature = sigMatch ? stripTags(sigMatch[1]) : name;

    const summaryMatch = docHtml.match(
      /<p class="fsdocs-summary">([\s\S]*?)<\/p>/,
    );
    const summary = summaryMatch ? stripTagsKeepCode(summaryMatch[1]) : "";

    const params: Parameter[] = [];
    const paramRegex =
      /<dt class="fsdocs-param">\s*<span class="fsdocs-param-name">\s*([\s\S]*?)\s*<\/span>\s*:\s*<code>([\s\S]*?)<\/code>\s*<\/dt>/g;
    let paramMatch: RegExpExecArray | null;
    while ((paramMatch = paramRegex.exec(docHtml)) !== null) {
      params.push({
        name: stripTags(paramMatch[1]),
        type: stripTags(paramMatch[2]),
      });
    }

    const returnMatch = docHtml.match(
      /<span class="fsdocs-return-name">[\s\S]*?<\/span>\s*<code>([\s\S]*?)<\/code>/,
    );
    const returns = returnMatch ? stripTags(returnMatch[1]) : "";

    if (name) {
      members.push({ name, signature, summary, params, returns });
    }
  }
  return members;
}

function extractTypeMembers(html: string): MemberSection[] {
  const types: MemberSection[] = [];
  const sectionRegex =
    /<h3>\s*(Union cases|Record fields|Instance members|Static members|Constructors)\s*<\/h3>\s*<table[\s\S]*?<tbody>([\s\S]*?)<\/tbody>/g;

  let match: RegExpExecArray | null;
  while ((match = sectionRegex.exec(html)) !== null) {
    const sectionName = match[1];
    const tbody = match[2];
    const members = extractMembers(
      `<table><tbody>${tbody}</tbody></table>`,
    );
    if (members.length > 0) {
      types.push({ section: sectionName, members });
    }
  }
  return types;
}

function extractModuleSummary(html: string): string {
  const match = html.match(
    /<div class="fsdocs-summary-contents">\s*<p class="fsdocs-summary">([\s\S]*?)<\/p>/,
  );
  return match ? stripTagsKeepCode(match[1]) : "";
}

// ── MDX generation ───────────────────────────────────────────────────

function h(level: number): string {
  return "#".repeat(level);
}

function generateSourceContent(html: string, sourceLevel: number): string {
  const summary = extractModuleSummary(html);

  const sections: SourceSection[] = [];
  const sectionRegex =
    /<h3>\s*(Functions and values|Types|Modules)\s*<\/h3>\s*<table[\s\S]*?<tbody>([\s\S]*?)<\/tbody>/g;

  let match: RegExpExecArray | null;
  while ((match = sectionRegex.exec(html)) !== null) {
    const sectionName = match[1];
    const tbody = match[2];
    const members = extractMembers(tbody);
    if (members.length > 0) {
      sections.push({ title: sectionName, members });
    }
  }

  const typeSections = extractTypeMembers(html);

  let mdx = "";
  const sectionLevel = sourceLevel + 1;
  const memberLevel = sectionLevel + 1;

  if (summary) {
    mdx += `${escapeMdx(summary)}\n\n`;
  }

  for (const section of typeSections) {
    mdx += `${h(sectionLevel)} ${section.section}\n\n`;
    mdx += "| Name | Description |\n";
    mdx += "| --- | --- |\n";
    for (const m of section.members) {
      const sig = escapeMdx(escapeTableCell(m.signature));
      const desc = escapeMdx(escapeTableCell(m.summary));
      mdx += `| \`${sig}\` | ${desc} |\n`;
    }
    mdx += "\n";
  }

  for (const section of sections) {
    if (section.title !== "Functions and values") {
      mdx += `${h(sectionLevel)} ${section.title}\n\n`;
    }

    mdx += "| Function | Description |\n";
    mdx += "| --- | --- |\n";

    for (const m of section.members) {
      const sig = escapeMdx(escapeTableCell(m.signature));
      const desc = escapeMdx(escapeTableCell(m.summary));
      mdx += `| \`${sig}\` | ${desc} |\n`;
    }

    mdx += "\n";

    for (let mi = 0; mi < section.members.length; mi++) {
      const m = section.members[mi];
      if (mi > 0) {
        mdx += `---\n\n`;
      }
      const level =
        section.title === "Functions and values"
          ? sectionLevel
          : memberLevel;
      mdx += `${h(level)} ${escapeMdx(m.name)}\n\n`;
      mdx += "```fsharp\n";
      mdx += m.signature;
      mdx += "\n```\n\n";

      if (m.summary) {
        mdx += `${escapeMdx(m.summary)}\n\n`;
      }

      if (m.params.length > 0) {
        mdx += "**Parameters**\n\n";
        for (const p of m.params) {
          mdx += `- \`${p.name}\` : \`${p.type}\`\n`;
        }
        mdx += "\n";
      }

      if (m.returns) {
        mdx += `**Returns** \`${m.returns}\`\n\n`;
      }
    }
  }

  return mdx;
}

function generateFileMdx(filePage: FilePage): string {
  const ns = filePage.namespace
    ? `\nnamespace: "${filePage.namespace}"`
    : "";
  const multi = filePage.sources.length > 1;
  const title = multi ? filePage.title : filePage.sources[0].heading;
  const showTitle = multi ? "\nshowTitle: false" : "";
  let mdx = `---\ntitle: "${title}"\ndescription: "${filePage.description}"${ns}${showTitle}\n---\n\n`;

  if (multi) {
    for (let i = 0; i < filePage.sources.length; i++) {
      const source = filePage.sources[i];
      const filePath = join(refDir, source.html);
      const html = readFileSync(filePath, "utf-8");

      if (i > 0) mdx += "---\n\n";
      mdx += `# ${source.heading}\n\n`;
      mdx += generateSourceContent(html, 1);
    }
  } else {
    const source = filePage.sources[0];
    const filePath = join(refDir, source.html);
    const html = readFileSync(filePath, "utf-8");
    mdx += generateSourceContent(html, 1);
  }

  return mdx;
}

// ── Main ─────────────────────────────────────────────────────────────

function main(): void {
  try {
    readdirSync(refDir);
  } catch {
    console.error(`Reference directory not found: ${refDir}`);
    console.error("Run 'dotnet fsdocs build' first.");
    process.exit(1);
  }

  const subdirs = new Set(
    filePages.map((p) => p.subdir).filter(Boolean),
  );
  for (const sub of subdirs) {
    mkdirSync(join(outDir, sub), { recursive: true });
  }

  for (const filePage of filePages) {
    try {
      const mdx = generateFileMdx(filePage);
      const dir = filePage.subdir ? join(outDir, filePage.subdir) : outDir;
      const outPath = join(dir, `${filePage.slug}.mdx`);
      writeFileSync(outPath, mdx);
      const label = filePage.subdir
        ? `${filePage.subdir}/${filePage.slug}.mdx`
        : `${filePage.slug}.mdx`;
      console.log(`  ${label}`);
    } catch (e: unknown) {
      const htmlList = filePage.sources.map((s) => s.html).join(", ");
      const message = e instanceof Error ? e.message : String(e);
      console.error(`Error processing ${htmlList}: ${message}`);
    }
  }
}

console.log("Generating API MDX files...");
main();
console.log("Done.");
