namespace FsTests

open Xunit
open FsUnit.Xunit
open FsHcl
open FsHcl.Hcl

module HclTests =
    open Render
    open Syntax
    open Values

    [<Fact>]
    let ``escapes HCL strings`` () =
        escapeString "path\\to\\\"file\"\n${ref}"
        |> should equal "path\\\\to\\\\\\\"file\\\"\\n$${ref}"

    [<Fact>]
    let ``renders typed values and raw expressions`` () =
        let result =
            hcl {
                block "resource" {
                    attr "name" (str "example")
                    attr "enabled" (bool true)
                    attr "count" (number 2m)
                    attr "ref" (raw "module.example.id")
                }
            }
            |> document

        result |> should haveSubstring "name    = \"example\""
        result |> should haveSubstring "enabled = true"
        result |> should haveSubstring "count   = 2"
        result |> should haveSubstring "ref     = module.example.id"

    [<Fact>]
    let ``supports labelled blocks and list items`` () =
        let result =
            hcl {
                blockWithLabels "module" [ "my_module" ] {
                    attr "source" (str "./my-module")

                    list_ "patterns" {
                        item (str "infra/**")
                    }
                }
            }
            |> document

        result |> should haveSubstring "module \"my_module\" {"
        result |> should haveSubstring "patterns = ["
        result |> should haveSubstring "\"infra/**\","

    [<Fact>]
    let ``renders empty labelled block on one line`` () =
        let result =
            hcl {
                blockWithLabels "data" [ "aws_caller_identity"; "current" ] {}
            }
            |> document

        result |> should equal "data \"aws_caller_identity\" \"current\" {}\n"

    [<Fact>]
    let ``renders a block containing only omitted attributes on one line`` () =
        hcl { block "feature" { optAttr "enabled" None } }
        |> document
        |> should equal "feature {}\n"

    [<Fact>]
    let ``renders jsonencode anonymous records`` () =
        let result =
            hcl {
                block "resource" {
                    attr
                        "template_body"
                        (jsonencode
                            {|
                                Name = "example"
                                Enabled = true
                                Ref = expr "local.example"
                                Nested =
                                    {|
                                        Value = "ok"
                                        Count = 2
                                    |}
                            |})
                }
            }
            |> document

        result |> should haveSubstring "template_body = jsonencode({"
        result |> should haveSubstring "Name    = \"example\""
        result |> should haveSubstring "Enabled = true"
        result |> should haveSubstring "Ref     = local.example"
        result |> should haveSubstring "Nested  = {"
        result |> should haveSubstring "Value = \"ok\""
        result |> should haveSubstring "Count = 2"

    [<Fact>]
    let ``renders jsonencode sequences`` () =
        let result =
            hcl {
                block "resource" {
                    attr "values" (jsonencode [ "a"; "b" ])
                }
            }
            |> document

        result |> should haveSubstring "values = jsonencode(["
        result |> should haveSubstring "\"a\","
        result |> should haveSubstring "\"b\","

    [<Fact>]
    let ``applies render options to nested values`` () =
        let options = {
            defaults with
                indentSize = 4
                alignAttributes = false
                trailingNewline = false
        }

        let result =
            hcl {
                block "locals" {
                    attr "config" (obj { stringField "long_name" "value"; boolField "on" true })
                }
            }
            |> withOptions options

        result
        |> should equal "locals {\n    config = {\n        long_name = \"value\"\n        on = true\n    }\n}"

    [<Fact>]
    let ``renders function calls with multiple arguments`` () =
        call "coalesce" [ raw "var.name"; str "fallback" ]
        |> attr "name"
        |> Render.node
        |> should equal "name = coalesce(var.name, \"fallback\")\n"

    [<Fact>]
    let ``builds nested object fields`` () =
        obj { objField "nested" { stringField "name" "example" } }
        |> attr "config"
        |> Render.node
        |> should haveSubstring "nested = {"

module TerraformHclTests =
    open Render
    open Syntax
    open TerraformHcl
    open Values
    [<Fact>]
    let ``terraform helpers cover syntax blocks`` () =
        let result =
            hcl {
                provider "tfe" { attr "hostname" (str "app.terraform.io") }

                resource "tfe_project" "project" {
                    attr "name" (str "example")
                }

                moved_ {
                    from_ "tfe_project.old"
                    to_ "tfe_project.project"
                }
            }
            |> document

        result |> should haveSubstring "provider \"tfe\" {"
        result |> should haveSubstring "resource \"tfe_project\" \"project\" {"
        result |> should haveSubstring "moved {"
        result |> should haveSubstring "from = tfe_project.old"
        result |> should haveSubstring "to   = tfe_project.project"
