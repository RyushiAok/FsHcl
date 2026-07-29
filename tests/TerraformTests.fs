namespace FsTests

open Xunit
open FsUnit.Xunit
open FsHcl.Hcl
open FsHcl.TerraformHcl

module TerraformHclTests =

    [<Fact>]
    let ``terraform helpers cover syntax blocks`` () =
        let result =
            hcl {
                provider "tfe" { attr "hostname" (str "app.terraform.io") }

                resource "tfe_project" "project" { attr "name" (str "example") }

                moved_ {
                    from_ "tfe_project.old"
                    to_ "tfe_project.project"
                }
            }
            |> document

        result |> should haveSubstring "provider \"tfe\" {"

        result
        |> should haveSubstring "resource \"tfe_project\" \"project\" {"

        result |> should haveSubstring "moved {"
        result |> should haveSubstring "from = tfe_project.old"
        result |> should haveSubstring "to   = tfe_project.project"
