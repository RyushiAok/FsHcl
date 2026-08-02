namespace FsTests

open Xunit
open FsUnit.Xunit
open FsHcl.Hcl

module CommentTests =

    [<Fact>]
    let ``renders line comments`` () =
        hcl {
            comment "This is a comment"
            block "resource" [] { attr "name" (str "example") }
        }
        |> document
        |> should haveSubstring "# This is a comment"

    [<Fact>]
    let ``renders block comments`` () =
        hcl { blockComment [ "Line 1"; "Line 2" ] }
        |> document
        |> should equal "/*\nLine 1\nLine 2\n*/\n"

    [<Fact>]
    let ``renders nested comments inside blocks`` () =
        let result =
            hcl {
                block "resource" [] {
                    comment "description of the attribute"
                    attr "name" (str "example")
                }
            }
            |> document

        result
        |> should haveSubstring "  # description of the attribute"

        result |> should haveSubstring "  name = \"example\""

module HeredocTests =

    [<Fact>]
    let ``renders heredoc strings`` () =
        attr "policy" (heredoc "EOF" "{\n  \"Version\": \"2012-10-17\"\n}")
        |> Render.node
        |> should equal "policy = <<EOF\n{\n  \"Version\": \"2012-10-17\"\n}\nEOF\n"

    [<Fact>]
    let ``renders indented heredoc strings`` () =
        let result =
            hcl { block "resource" [] { attr "content" (heredocIndent "SCRIPT" "#!/bin/bash\necho hello") } }
            |> document

        result |> should haveSubstring "<<-SCRIPT"
        result |> should haveSubstring "#!/bin/bash\necho hello"
        result |> should haveSubstring "SCRIPT"

module TemplateAndExpressionTests =

    [<Fact>]
    let ``renders template strings with interpolation`` () =
        attr "name" (templateStr "${var.prefix}-instance")
        |> Render.node
        |> should equal "name = \"${var.prefix}-instance\"\n"

    [<Fact>]
    let ``renders conditional expressions`` () =
        attr "count" (conditional "var.enabled" "1" "0")
        |> Render.node
        |> should equal "count = var.enabled ? 1 : 0\n"

    [<Fact>]
    let ``renders for-tuple expressions`` () =
        attr "ids" (forTuple "v" "var.instances" "v.id")
        |> Render.node
        |> should equal "ids = [for v in var.instances : v.id]\n"

    [<Fact>]
    let ``renders for-tuple expressions with condition`` () =
        attr "ids" (forTupleIf "v" "var.instances" "v.id" "v.enabled")
        |> Render.node
        |> should equal "ids = [for v in var.instances : v.id if v.enabled]\n"

    [<Fact>]
    let ``renders for-object expressions`` () =
        attr "map" (forObject "k" "v" "var.items" "k" "v.value")
        |> Render.node
        |> should equal "map = {for k, v in var.items : k => v.value}\n"

    [<Fact>]
    let ``renders for-object expressions with grouping`` () =
        attr "grouped" (forObjectGroup "k" "v" "var.items" "k" "v.value")
        |> Render.node
        |> should equal "grouped = {for k, v in var.items : k => v.value...}\n"

    [<Fact>]
    let ``renders for-object expressions with condition`` () =
        attr "filtered" (forObjectIf "k" "v" "var.items" "k" "v.value" "v.enabled")
        |> Render.node
        |> should equal "filtered = {for k, v in var.items : k => v.value if v.enabled}\n"

    [<Fact>]
    let ``extended interpolation (FS-1132) preserves HCL template directives`` () =
        let env = "prod"

        let result =
            hcl {
                attr "greeting" (templateStr $$"""%{if var.name}Hello, ${var.name} ({{env}})%{endif}""")
                attr "items" (templateStr $$"""%{for ip in var.ips}${ip}%{endfor}""")
            }
            |> document

        result
        |> should haveSubstring "greeting = \"%{if var.name}Hello, ${var.name} (prod)%{endif}\""

        result
        |> should haveSubstring "items    = \"%{for ip in var.ips}${ip}%{endfor}\""
