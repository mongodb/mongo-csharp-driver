# AWS Lambda Testing

This project contains source code and supporting files for a serverless application that you can deploy with the SAM CLI. It includes the following files and folders.

- MongoDB.Driver.LambdaTest - Code for the application's Lambda function.
- template.yaml - A template that defines the application's AWS resources.

The application uses several AWS resources, including Lambda functions and an API Gateway API. These resources are defined in the `template.yaml` file in this project. You can update the template to add AWS resources through the same deployment process that updates the application code.

## Running Locally

Prerequisites:

- AWS SAM CLI
- Docker daemon running with mongodb instance

Build the application with the `sam build` command from the `tests/FaasTests/LambdaTests` folder.

```bash
sam build
```

The SAM CLI installs dependencies defined in `./MongoDB.Driver.LambdaTest/MongoDB.Driver.LambdaTest.csproj`, creates a deployment package, and saves it in a `.aws-sam/build` folder.

Run the function locally and invoke them with the `sam local invoke` command.

```bash
sam local invoke --parameter-overrides "MongoDbUri=mongodb://host.docker.internal:27017"
```

## Resources

See the [AWS SAM developer guide](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/what-is-sam.html) for an introduction to SAM specification, the SAM CLI, and serverless application concepts.
