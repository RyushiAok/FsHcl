namespace FsHcl.Tests

open Xunit
open FsUnit.Xunit
open FsHcl

module HclTests =
    [<Fact>]
    let ``escapes HCL strings`` () =
        Hcl.escapeString "path\\to\\\"file\"\n${ref}"
        |> should equal "path\\\\to\\\\\\\"file\\\"\\n$${ref}"

    [<Fact>]
    let ``renders typed values and raw expressions`` () =
        let result =
            Hcl.hcl {
                Hcl.block "resource" {
                    Hcl.attr "name" (Hcl.str "example")
                    Hcl.attr "enabled" (Hcl.bool true)
                    Hcl.attr "count" (Hcl.number 2m)
                    Hcl.attr "ref" (Hcl.raw "module.example.id")
                }
            }
            |> Hcl.render

        result |> should haveSubstring "name    = \"example\""
        result |> should haveSubstring "enabled = true"
        result |> should haveSubstring "count   = 2"
        result |> should haveSubstring "ref     = module.example.id"

    [<Fact>]
    let ``supports labelled blocks and list items`` () =
        let result =
            Hcl.hcl {
                Hcl.blockWithLabels "module" [ "my_module" ] {
                    Hcl.attr "source" (Hcl.str "./my-module")

                    Hcl.list_ "patterns" {
                        Hcl.item (Hcl.str "infra/**")
                    }
                }
            }
            |> Hcl.render

        result |> should haveSubstring "module \"my_module\" {"
        result |> should haveSubstring "patterns = ["
        result |> should haveSubstring "\"infra/**\","

module TerraformHclTests =
    [<Fact>]
    let ``terraform helpers cover syntax blocks`` () =
        let result =
            Hcl.hcl {
                TerraformHcl.provider "tfe" { Hcl.attr "hostname" (Hcl.str "app.terraform.io") }

                TerraformHcl.resource "tfe_project" "project" {
                    Hcl.attr "name" (Hcl.str "example")
                }

                TerraformHcl.moved_ {
                    TerraformHcl.from_ "tfe_project.old"
                    TerraformHcl.to_ "tfe_project.project"
                }
            }
            |> Hcl.render

        result |> should haveSubstring "provider \"tfe\" {"
        result |> should haveSubstring "resource \"tfe_project\" \"project\" {"
        result |> should haveSubstring "moved {"
        result |> should haveSubstring "from = tfe_project.old"
        result |> should haveSubstring "to   = tfe_project.project"
