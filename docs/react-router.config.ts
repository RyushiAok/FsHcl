import type { Config } from "@react-router/dev/config";

export default {
  ssr: false,
  basename: "/FsHcl/",
  prerender: [
    "/",
    "/getting-started",
    "/values",
    "/syntax",
    "/terraform",
    "/patterns",
  ],
} satisfies Config;
