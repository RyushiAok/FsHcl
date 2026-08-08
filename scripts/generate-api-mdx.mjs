import { readFileSync, writeFileSync, readdirSync, mkdirSync } from "node:fs";
import { join, basename } from "node:path";

const root = join(import.meta.dirname, "..");
const refDir = join(root, ".fsdocs-out", "reference");
const outDir = join(root, "docs", "app", "content");

const modulePages = {
  "fshcl-hcl-values.html": {
    slug: "api-values",
    title: "Values Module",
    description: "Value constructors and conversion functions.",
  },
  "fshcl-hcl-syntax.html": {
    slug: "api-syntax",
    title: "Syntax Module",
    description: "HCL block and attribute builder functions.",
  },
  "fshcl-hcl-render.html": {
    slug: "api-render",
    title: "Render Module",
    description: "HCL rendering functions.",
  },
  "fshcl-terraformhcl.html": {
    slug: "api-terraform",
    title: "TerraformHcl Module",
    description: "Terraform-specific block and attribute helpers.",
  },
  "fshcl-hcl.html": {
    slug: "api-types",
    title: "Types",
    description: "Core HCL types: Value, Node, Expr, RenderOptions.",
  },
};

function decodeEntities(text) {
  return text
    .replace(/&#32;/g, " ")
    .replace(/&#39;/g, "'")
    .replace(/&amp;/g, "&")
    .replace(/&lt;/g, "<")
    .replace(/&gt;/g, ">")
    .replace(/&quot;/g, '"');
}

function stripTags(html) {
  return decodeEntities(html.replace(/<[^>]+>/g, "")).trim();
}

function extractMembers(html) {
  const members = [];
  const tableRegex =
    /<tr>\s*<td class="fsdocs-member-usage">([\s\S]*?)<\/td>\s*<td class="fsdocs-member-xmldoc">([\s\S]*?)<\/td>\s*<\/tr>/g;

  let match;
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
    const summary = summaryMatch ? stripTags(summaryMatch[1]) : "";

    const params = [];
    const paramRegex =
      /<dt class="fsdocs-param">\s*<span class="fsdocs-param-name">\s*([\s\S]*?)\s*<\/span>\s*:\s*<code>([\s\S]*?)<\/code>\s*<\/dt>/g;
    let paramMatch;
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

function extractTypeMembers(html) {
  const types = [];
  const sectionRegex =
    /<h3>\s*(Union cases|Record fields|Instance members|Static members|Constructors)\s*<\/h3>\s*<table[\s\S]*?<tbody>([\s\S]*?)<\/tbody>/g;

  let match;
  while ((match = sectionRegex.exec(html)) !== null) {
    const sectionName = match[1];
    const tbody = match[2];
    const members = extractMembers(
      `<table><tbody>${tbody}</tbody></table>`.replace(
        /fsdocs-member-usage/g,
        "fsdocs-member-usage",
      ),
    );
    if (members.length > 0) {
      types.push({ section: sectionName, members });
    }
  }
  return types;
}

function extractModuleSummary(html) {
  const match = html.match(
    /<div class="fsdocs-summary-contents">\s*<p class="fsdocs-summary">([\s\S]*?)<\/p>/,
  );
  return match ? stripTags(match[1]) : "";
}

function extractSectionTitle(html) {
  const match = html.match(/<h2>\s*([\s\S]*?)\s*<\/h2>/);
  return match ? stripTags(match[1]) : "";
}

function generateModuleMdx(pageInfo, html) {
  const summary = extractModuleSummary(html);

  const sections = [];
  const sectionRegex =
    /<h3>\s*(Functions and values|Types|Modules)\s*<\/h3>\s*<table[\s\S]*?<tbody>([\s\S]*?)<\/tbody>/g;

  let match;
  while ((match = sectionRegex.exec(html)) !== null) {
    const sectionName = match[1];
    const tbody = match[2];
    const members = extractMembers(
      tbody.replace(/(<tr>)/g, "$1").replace(/(<\/tr>)/g, "$1"),
    );
    if (members.length > 0) {
      sections.push({ title: sectionName, members });
    }
  }

  let mdx = `---\ntitle: "${pageInfo.title}"\ndescription: "${pageInfo.description}"\n---\n\n`;

  if (summary) {
    mdx += `${summary}\n\n`;
  }

  for (const section of sections) {
    if (section.title !== "Functions and values") {
      mdx += `## ${section.title}\n\n`;
    }

    mdx += "| Function | Description |\n";
    mdx += "| --- | --- |\n";

    for (const m of section.members) {
      const sig = m.signature.replace(/\|/g, "\\|");
      const desc = m.summary.replace(/\|/g, "\\|");
      mdx += `| \`${sig}\` | ${desc} |\n`;
    }

    mdx += "\n";

    for (let mi = 0; mi < section.members.length; mi++) {
      const m = section.members[mi];
      if (mi > 0) {
        mdx += `---\n\n`;
      }
      mdx += `### ${m.name}\n\n`;
      mdx += "```fsharp\n";
      mdx += m.signature;
      mdx += "\n```\n\n";

      if (m.summary) {
        mdx += `${m.summary}\n\n`;
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

function generateTypesMdx(html) {
  let mdx = `---\ntitle: "Types"\ndescription: "Core HCL types: Value, Node, Expr, RenderOptions."\n---\n\n`;

  const typeFiles = [
    "fshcl-hcl-value.html",
    "fshcl-hcl-node.html",
    "fshcl-hcl-expr.html",
    "fshcl-hcl-renderoptions.html",
  ];

  for (const file of typeFiles) {
    const filePath = join(refDir, file);
    try {
      const typeHtml = readFileSync(filePath, "utf-8");
      const title = extractSectionTitle(typeHtml);
      const summary = extractModuleSummary(typeHtml);
      const sections = extractTypeMembers(typeHtml);

      mdx += `## ${title}\n\n`;
      if (summary) {
        mdx += `${summary}\n\n`;
      }

      for (const section of sections) {
        mdx += `### ${section.section}\n\n`;
        mdx += "| Name | Description |\n";
        mdx += "| --- | --- |\n";
        for (const m of section.members) {
          const sig = m.signature.replace(/\|/g, "\\|");
          const desc = m.summary.replace(/\|/g, "\\|");
          mdx += `| \`${sig}\` | ${desc} |\n`;
        }
        mdx += "\n";
      }
    } catch {
      // skip missing files
    }
  }

  return mdx;
}

function main() {
  let files;
  try {
    files = readdirSync(refDir);
  } catch {
    console.error(`Reference directory not found: ${refDir}`);
    console.error("Run 'dotnet fsdocs build' first.");
    process.exit(1);
  }

  for (const [filename, pageInfo] of Object.entries(modulePages)) {
    const filePath = join(refDir, filename);
    try {
      const html = readFileSync(filePath, "utf-8");

      let mdx;
      if (pageInfo.slug === "api-types") {
        mdx = generateTypesMdx(html);
      } else {
        mdx = generateModuleMdx(pageInfo, html);
      }

      const outPath = join(outDir, `${pageInfo.slug}.mdx`);
      writeFileSync(outPath, mdx);
      console.log(`  ${pageInfo.slug}.mdx`);
    } catch (e) {
      console.error(`Error processing ${filename}: ${e.message}`);
    }
  }
}

console.log("Generating API MDX files...");
main();
console.log("Done.");
