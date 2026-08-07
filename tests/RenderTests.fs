namespace FsTests

open Xunit
open FsUnit.Xunit
open FsHcl.Hcl

module RenderTests =

    [<Fact>]
    let ``escapes HCL strings`` () =
        escapeString "path\\to\\\"file\"\n${ref}"
        |> should equal "path\\\\to\\\\\\\"file\\\"\\n$${ref}"

    [<Fact>]
    let ``renders typed values and raw expressions`` () =
        let result =
            hcl {
                block "resource" [] {
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
                block "module" [ "my_module" ] {
                    attr "source" (str "./my-module")

                    list_ "patterns" [ str "infra/**" ]
                }
            }
            |> document

        result |> should haveSubstring "module \"my_module\" {"
        result |> should haveSubstring "patterns = ["
        result |> should haveSubstring "\"infra/**\","

    [<Fact>]
    let ``renders empty labelled block on one line`` () =
        let result =
            hcl { block "data" [ "aws_caller_identity"; "current" ] { } }
            |> document

        result
        |> should equal "data \"aws_caller_identity\" \"current\" {}\n"

    [<Fact>]
    let ``renders a block containing only omitted attributes on one line`` () =
        hcl { block "feature" [] { optAttr "enabled" None } }
        |> document
        |> should equal "feature {}\n"

    [<Fact>]
    let ``renders jsonencode anonymous records`` () =
        let result =
            hcl {
                block "resource" [] {
                    attr
                        "template_body"
                        (jsonencode {|
                            Name = "example"
                            Enabled = true
                            Ref = expr "local.example"
                            Nested = {| Value = "ok"; Count = 2 |}
                        |})
                }
            }
            |> document

        result
        |> should haveSubstring "template_body = jsonencode({"

        result |> should haveSubstring "Name    = \"example\""
        result |> should haveSubstring "Enabled = true"
        result |> should haveSubstring "Ref     = local.example"
        result |> should haveSubstring "Nested  = {"
        result |> should haveSubstring "Value = \"ok\""
        result |> should haveSubstring "Count = 2"

    [<Fact>]
    let ``renders jsonencode sequences`` () =
        let result =
            hcl { block "resource" [] { attr "values" (jsonencode [ "a"; "b" ]) } }
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
                block "locals" [] {
                    attr
                        "config"
                        (obj {
                            stringField "long_name" "value"
                            boolField "on" true
                        })
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

    [<Fact>]
    let ``renders null value`` () =
        attr "value" null_
        |> Render.node
        |> should equal "value = null\n"

    [<Fact>]
    let ``renders empty object value`` () =
        attr "tags" (obj { () })
        |> Render.node
        |> should equal "tags = {}\n"

    [<Fact>]
    let ``renders empty list value`` () =
        attr "items" (Value.List [])
        |> Render.node
        |> should equal "items = []\n"

    [<Fact>]
    let ``renders join separating nodes with blank lines`` () =
        [ block "a" [] { attr "x" (number 1) }; block "b" [] { attr "y" (number 2) } ]
        |> Render.join
        |> should equal "a {\n  x = 1\n}\n\nb {\n  y = 2\n}\n"

    [<Fact>]
    let ``supports for loop in computation expression`` () =
        let names = [ "alpha"; "beta" ]

        let result =
            hcl {
                for name in names do
                    attr name (str name)
            }
            |> document

        result |> should haveSubstring "alpha = \"alpha\""
        result |> should haveSubstring "beta  = \"beta\""

    [<Fact>]
    let ``aligns attributes per group separated by blank lines`` () =
        let result =
            hcl {
                block "resource" [ "aws_instance"; "web" ] {
                    attr "ami" (str "abc-123")
                    attr "instance_type" (str "t2.micro")
                    blank
                    attr "availability_zone" (str "us-west-2a")
                    attr "key_name" (str "my-key")
                }
            }
            |> document

        result |> should haveSubstring "ami           = \"abc-123\""
        result |> should haveSubstring "instance_type = \"t2.micro\""
        result |> should haveSubstring "availability_zone = \"us-west-2a\""
        result |> should haveSubstring "key_name          = \"my-key\""
