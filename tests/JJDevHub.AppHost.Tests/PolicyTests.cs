using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;

using JJDevHub.ServiceDefaults;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace JJDevHub.AppHost.Tests
{
    public sealed class PolicyTests
    {
        [Fact]
        public async Task Owner_passes_student_policy()
        {
            await using var host = await HostAsync();
            var response = await SendAsync(host, "/student", Roles.Owner);

            Assert.Equal(HttpStatusCode.OK, response);
        }

        [Fact]
        public async Task Student_fails_owner_policy()
        {
            await using var host = await HostAsync();
            var response = await SendAsync(host, "/owner", Roles.Student);

            Assert.Equal(HttpStatusCode.Forbidden, response);
        }

        private static async Task<WebApplication> HostAsync()
        {
            using var rsa = RSA.Create(2048);
            var pem = rsa.ExportPkcs8PrivateKeyPem();
            var builder = WebApplication.CreateSlimBuilder();
            builder.Environment.EnvironmentName = Environments.Development;
            builder.WebHost.UseTestServer();
            builder.Configuration["Identity:SigningKeyPem"] = pem;
            builder.Configuration["Identity:Issuer"] = "JJDevHub";
            builder.Configuration["Identity:Audience"] = "JJDevHub";
            builder.AddDefaultAuthentication();
            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapGet("/student", () => Results.Ok()).RequireAuthorization(Policies.Student);
            app.MapGet("/owner", () => Results.Ok()).RequireAuthorization(Policies.Owner);
            await app.StartAsync();
            return app;
        }

        private static async Task<HttpStatusCode> SendAsync(WebApplication app, string path, string role)
        {
            var pem = app.Configuration["Identity:SigningKeyPem"] ?? "";
            using var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                issuer: "JJDevHub",
                audience: "JJDevHub",
                claims: [new Claim("sub", Guid.NewGuid().ToString()), new Claim("role", role)],
                notBefore: DateTime.UtcNow.AddMinutes(-1),
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256)));
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await app.GetTestClient().SendAsync(request);
            return response.StatusCode;
        }
    }
}
