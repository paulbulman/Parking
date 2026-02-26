terraform {
  backend "s3" {
    bucket = "paulbulman-terraform-state"
    region = "eu-west-2"
  }
}
