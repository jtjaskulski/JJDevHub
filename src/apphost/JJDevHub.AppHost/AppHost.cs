var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.JJDevHub_Gateway>("gateway");

builder.Build().Run();