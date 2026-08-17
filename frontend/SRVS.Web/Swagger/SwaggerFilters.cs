using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SRVS.Web.DTOs;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SRVS.Web.Swagger;

/// <summary>
/// Provides realistic example values for OpenAPI schemas in Swagger UI.
/// </summary>
public class SwaggerExampleSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        try
        {
            if (schema?.Properties == null) return;

            foreach (var property in schema.Properties)
            {
                var name = property.Key;
                var propSchema = property.Value;
                if (propSchema == null) continue;

                switch (name.ToLowerInvariant())
                {
                    case "schoolid":
                        propSchema.Example = new OpenApiString("10001");
                        break;
                    case "studentid":
                        propSchema.Example = new OpenApiString("2026000001");
                        break;
                    case "instructorid":
                        propSchema.Example = new OpenApiString("10001");
                        break;
                    case "password":
                    case "newpassword":
                    case "confirmpassword":
                        propSchema.Example = new OpenApiString("Giangwapo123?");
                        break;
                    case "email":
                        propSchema.Example = new OpenApiString("jane.doe@edu.ph");
                        break;
                    case "firstname":
                        propSchema.Example = new OpenApiString("Jane");
                        break;
                    case "lastname":
                        propSchema.Example = new OpenApiString("Doe");
                        break;
                    case "fullname":
                    case "studentfullname":
                        propSchema.Example = new OpenApiString("Jane Doe");
                        break;
                    case "departmentname":
                        propSchema.Example = new OpenApiString("Computer Engineering");
                        break;
                    case "coursecode":
                    case "subjectcode":
                        propSchema.Example = new OpenApiString("CPE 101");
                        break;
                    case "coursetitle":
                    case "subjecttitle":
                    case "syllabustitle":
                    case "assignedsyllabustitle":
                        propSchema.Example = new OpenApiString("Computer Engineering Systems");
                        break;
                    case "academicyear":
                        propSchema.Example = new OpenApiString("2025-2026");
                        break;
                    case "semester":
                        propSchema.Example = new OpenApiString("First Semester");
                        break;
                    case "filename":
                    case "currentfilename":
                        propSchema.Example = new OpenApiString("CPE101_Syllabus.pdf");
                        break;
                    case "changesummary":
                        propSchema.Example = new OpenApiString("Initial syllabus draft creation.");
                        break;
                    case "remarks":
                    case "reviewerremarks":
                    case "reviewremarks":
                        propSchema.Example = new OpenApiString("Approved for academic curriculum distribution.");
                        break;
                    case "refreshtoken":
                        propSchema.Example = new OpenApiString("sample-refresh-token-xyz123");
                        break;
                    case "id":
                    case "documentid":
                    case "syllabusid":
                    case "syllabusdocid":
                    case "assignedsyllabusid":
                        propSchema.Example = new OpenApiString("12345");
                        break;
                }
            }
        }
        catch
        {
            // Ignore filter errors to prevent OpenAPI generator crashes
        }
    }
}

/// <summary>
/// Enhances parameter documentation and examples for API endpoints in Swagger UI.
/// </summary>
public class SwaggerParameterOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        try
        {
            if (operation?.Parameters == null) return;

            foreach (var parameter in operation.Parameters)
            {
                if (parameter == null) continue;

                switch (parameter.Name?.ToLowerInvariant())
                {
                    case "id":
                    case "syllabusid":
                    case "syllabusdocumentid":
                    case "versionid":
                    case "documentid":
                        if (parameter.In == ParameterLocation.Path)
                        {
                            parameter.Description = "Syllabus Document ID (5-digit number like 12345 or 12112)";
                            parameter.Schema.Type = "string";
                            parameter.Example = new OpenApiString("12345");
                        }
                        break;
                    case "status":
                        parameter.Description = "Filter by syllabus status: 0 = Draft, 1 = Submitted, 2 = Approved, 3 = Rejected";
                        parameter.Example = new OpenApiInteger(1);
                        break;
                    case "search":
                        parameter.Description = "Search keyword (name, email, or ID)";
                        parameter.Example = new OpenApiString("Jane");
                        break;
                    case "term":
                        parameter.Description = "Search keyword for syllabus course code or title";
                        parameter.Example = new OpenApiString("CPE");
                        break;
                    case "maxresults":
                        parameter.Description = "Maximum number of search results (default 100)";
                        parameter.Example = new OpenApiInteger(10);
                        break;
                }
            }
        }
        catch
        {
            // Ignore filter errors to prevent OpenAPI generator crashes
        }
    }
}

/// <summary>
/// Handles file upload parameters for multipart form-data endpoints in Swagger UI.
/// </summary>
public class SwaggerFileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        try
        {
            var formParams = context.MethodInfo.GetParameters()
                .Where(p => p.ParameterType == typeof(UploadSyllabusFormRequest))
                .ToList();

            if (!formParams.Any()) return;

            operation.RequestBody = new OpenApiRequestBody
            {
                Description = "Upload syllabus file along with metadata",
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = context.SchemaGenerator.GenerateSchema(typeof(UploadSyllabusFormRequest), context.SchemaRepository)
                    }
                }
            };
        }
        catch
        {
            // Fallback safely
        }
    }
}
