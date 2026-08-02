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

    [<Fact>]
    let ``dynamic block wraps body in content`` () =
        let result =
            hcl {
                resource "aws_security_group" "example" {
                    dynamic_ "ingress" "var.rules" {
                        attr "from_port" (raw "ingress.value.port")
                        attr "to_port" (raw "ingress.value.port")
                        attr "protocol" (str "tcp")
                    }
                }
            }
            |> document

        result |> should haveSubstring "dynamic \"ingress\" {"
        result |> should haveSubstring "for_each = var.rules"
        result |> should haveSubstring "content {"
        result |> should haveSubstring "from_port = ingress.value.port"
        result |> should haveSubstring "protocol  = \"tcp\""

    [<Fact>]
    let ``dynamic block with custom iterator`` () =
        let result =
            hcl {
                resource "aws_elastic_beanstalk_environment" "env" {
                    dynamicWithIterator "setting" "var.settings" "s" {
                        attr "namespace" (raw "s.value.namespace")
                        attr "name" (raw "s.value.name")
                    }
                }
            }
            |> document

        result |> should haveSubstring "dynamic \"setting\" {"
        result |> should haveSubstring "for_each = var.settings"
        result |> should haveSubstring "iterator = s"
        result |> should haveSubstring "content {"

    [<Fact>]
    let ``meta-arguments render correctly`` () =
        let result =
            hcl {
                resource "aws_instance" "web" {
                    count (number 3)
                    for_each (raw "var.instances")
                    depends_on [ "aws_iam_role.example"; "aws_s3_bucket.data" ]
                    provider_ "aws.west"
                }
            }
            |> document

        result |> should haveSubstring "count      = 3"
        result |> should haveSubstring "for_each   = var.instances"
        result |> should haveSubstring "depends_on = ["
        result |> should haveSubstring "aws_iam_role.example,"
        result |> should haveSubstring "aws_s3_bucket.data,"
        result |> should haveSubstring "provider   = aws.west"

    [<Fact>]
    let ``lifecycle block with helpers`` () =
        let result =
            hcl {
                resource "aws_instance" "example" {
                    lifecycle {
                        create_before_destroy true
                        prevent_destroy true
                        ignore_changes [ "tags"; "name" ]
                        replace_triggered_by [ "aws_instance.other.id" ]
                    }
                }
            }
            |> document

        result |> should haveSubstring "lifecycle {"
        result |> should haveSubstring "create_before_destroy = true"
        result |> should haveSubstring "prevent_destroy       = true"
        result |> should haveSubstring "ignore_changes        = ["
        result |> should haveSubstring "tags,"
        result |> should haveSubstring "replace_triggered_by  = ["

    [<Fact>]
    let ``lifecycle ignore_changes all`` () =
        let result =
            hcl {
                resource "aws_instance" "example" {
                    lifecycle { ignore_changes_all }
                }
            }
            |> document

        result |> should haveSubstring "ignore_changes = all"

    [<Fact>]
    let ``precondition and postcondition blocks`` () =
        let result =
            hcl {
                resource "aws_instance" "example" {
                    lifecycle {
                        precondition {
                            condition_ "var.instance_type != \"\""
                            error_message "instance_type must not be empty"
                        }

                        postcondition {
                            condition_ "self.public_ip != \"\""
                            error_message "must have a public IP"
                        }
                    }
                }
            }
            |> document

        result |> should haveSubstring "precondition {"
        result |> should haveSubstring "condition     = var.instance_type"
        result |> should haveSubstring "error_message = \"instance_type must not be empty\""
        result |> should haveSubstring "postcondition {"
        result |> should haveSubstring "error_message = \"must have a public IP\""

    [<Fact>]
    let ``terraform sub-blocks render correctly`` () =
        let result =
            hcl {
                terraform {
                    required_version ">= 1.6.0"

                    required_providers {
                        object_ "aws" {
                            attr "source" (str "hashicorp/aws")
                            attr "version" (str "~> 5.0")
                        }
                    }

                    backend "s3" {
                        attr "bucket" (str "my-state")
                        attr "key" (str "infra/terraform.tfstate")
                    }
                }
            }
            |> document

        result |> should haveSubstring "terraform {"
        result |> should haveSubstring "required_version = \">= 1.6.0\""
        result |> should haveSubstring "required_providers {"
        result |> should haveSubstring "source  = \"hashicorp/aws\""
        result |> should haveSubstring "version = \"~> 5.0\""
        result |> should haveSubstring "backend \"s3\" {"
        result |> should haveSubstring "bucket = \"my-state\""

    [<Fact>]
    let ``cloud block inside terraform`` () =
        let result =
            hcl {
                terraform {
                    cloud {
                        attr "organization" (str "my-org")

                        block "workspaces" { attr "name" (str "production") }
                    }
                }
            }
            |> document

        result |> should haveSubstring "cloud {"
        result |> should haveSubstring "organization = \"my-org\""
        result |> should haveSubstring "workspaces {"
        result |> should haveSubstring "name = \"production\""

    [<Fact>]
    let ``variable block with type, default, and validation`` () =
        let result =
            hcl {
                variable "instance_type" {
                    type_ "string"
                    default_ (str "t3.micro")
                    description "EC2 instance type"
                    sensitive false
                    nullable false

                    validation {
                        condition_ "contains([\"t3.micro\", \"t3.small\"], var.instance_type)"
                        error_message "Must be t3.micro or t3.small."
                    }
                }
            }
            |> document

        result |> should haveSubstring "variable \"instance_type\" {"
        result |> should haveSubstring "type        = string"
        result |> should haveSubstring "default     = \"t3.micro\""
        result |> should haveSubstring "description = \"EC2 instance type\""
        result |> should haveSubstring "sensitive   = false"
        result |> should haveSubstring "nullable    = false"
        result |> should haveSubstring "validation {"
        result |> should haveSubstring "error_message = \"Must be t3.micro or t3.small.\""

    [<Fact>]
    let ``output block with value and sensitive`` () =
        let result =
            hcl {
                output "db_password" {
                    value_ (raw "aws_db_instance.main.password")
                    description "The database password"
                    sensitive true
                }
            }
            |> document

        result |> should haveSubstring "output \"db_password\" {"
        result |> should haveSubstring "value       = aws_db_instance.main.password"
        result |> should haveSubstring "description = \"The database password\""
        result |> should haveSubstring "sensitive   = true"

    [<Fact>]
    let ``provisioner and connection blocks`` () =
        let result =
            hcl {
                resource "aws_instance" "web" {
                    connection {
                        attr "type" (str "ssh")
                        attr "host" (raw "self.public_ip")
                    }

                    provisioner "remote-exec" {
                        attr "inline" (arr { raw "echo hello" })
                    }
                }
            }
            |> document

        result |> should haveSubstring "connection {"
        result |> should haveSubstring "type = \"ssh\""
        result |> should haveSubstring "host = self.public_ip"
        result |> should haveSubstring "provisioner \"remote-exec\" {"
