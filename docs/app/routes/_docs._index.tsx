import { getDoc } from "~/content/mdx.server";
import { MdxContent } from "~/ui/mdx/mdx-components";
import { article } from "~/ui/mdx/mdx-style.css";
import { TableOfContents } from "~/ui/toc";
import { heroSection, heroSubtitle, heroTitle } from "./_docs._index.css";

export async function loader() {
  return getDoc("index");
}

export function meta() {
  return [
    { title: "FsHcl -- Generate HCL from F#" },
    {
      name: "description",
      content: "A typed DSL that generates HCL / Terraform code from F#.",
    },
  ];
}

export default function Landing({ loaderData }: { loaderData: Awaited<ReturnType<typeof loader>> }) {
  const { code, toc } = loaderData;
  return (
    <>
      <article className={article}>
        <div className={heroSection}>
          <h1 className={heroTitle}>FsHcl</h1>
          <div className={heroSubtitle}>
            A typed DSL that generates HCL / Terraform code from F#.
          </div>
        </div>
        <MdxContent code={code} />
      </article>
      <TableOfContents entries={toc} />
    </>
  );
}
