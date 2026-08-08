import { Flex, Heading, Text } from "@radix-ui/themes";
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
        <Flex direction="column" gap="1" className={heroSection}>
          <Heading size="8" className={heroTitle}>
            FsHcl
          </Heading>
          <Text size="4" className={heroSubtitle}>
            A typed DSL that generates HCL / Terraform code from F#.
          </Text>
        </Flex>
        <MdxContent code={code} />
      </article>
      <TableOfContents entries={toc} />
    </>
  );
}
