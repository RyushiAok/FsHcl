# FsHcl

A small typed HCL generation DSL for F#.

The generic `Hcl` module provides:

- typed values: string, bool, number, raw expression
- block, object, list, attribute, item, and blank-line nodes
- computation expression builders
- configurable rendering

`TerraformHcl` adds helpers for Terraform syntax elements such as `terraform`, `provider`, `resource`, `data`, `variable`, `output`, `module_`, `import_`, and `moved_`.
Provider-specific or module-specific arguments such as `project_name` should be defined by callers with `Hcl.attr`.

```fsharp
open FsHcl
open FsHcl.Hcl
open FsHcl.TerraformHcl

hcl {
    module_ "example" {
        attr "source" (str "./modules/example")

        list_ "patterns" {
            item (str "infra/**")
        }
    }
}
|> render
```
