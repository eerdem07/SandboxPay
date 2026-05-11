using Microsoft.AspNetCore.Mvc;
using SandboxPay.Api.Modules.MockPos.Application;
using SandboxPay.Api.Modules.MockPos.Support;
using SandboxPay.Api.Modules.MockPos.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IAuthorizePaymentService, AuthorizePaymentService>();
builder.Services.AddSingleton<IPosIdGenerator, PosIdGenerator>();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
        new BadRequestObjectResult(ValidationErrorResponse.FromModelState(context.ModelState));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
