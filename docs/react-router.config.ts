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
    "/api-types",
    "/api-values",
    "/api-syntax",
    "/api-render",
    "/api-terraform",
  ],
} satisfies Config;
