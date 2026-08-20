using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SyllabusRepository.DTOs;
using SRVS.Web.Components.Admin.Models;
using SRVS.Web.DTOs;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

            ApplyRequiredProperties(schema, context.Type);

            foreach (var property in schema.Properties)
            {
                var name = property.Key;
                var propSchema = property.Value;
                if (propSchema == null) continue;

                ApplyPropertyDescription(context.Type, name, propSchema);

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

    private static void ApplyRequiredProperties(OpenApiSchema schema, Type type)
    {
        var requiredProperties = type.GetProperties()
            .Where(p => p.GetCustomAttribute<RequiredAttribute>() is not null)
            .Select(ToJsonPropertyName)
            .Where(name => schema.Properties.ContainsKey(name))
            .ToList();

        requiredProperties.AddRange(type.Name switch
        {
            nameof(AssignRequest) => new[] { "studentId", "syllabusId" },
            nameof(BulkAssignRequest) => new[] { "studentIds", "syllabusId" },
            _ => Array.Empty<string>()
        });

        foreach (var propertyName in requiredProperties.Distinct())
        {
            if (schema.Properties.ContainsKey(propertyName))
            {
                schema.Required.Add(propertyName);
            }
        }
    }

    private static void ApplyPropertyDescription(Type type, string jsonPropertyName, OpenApiSchema propertySchema)
    {
        var typeName = type.Name;
        var name = jsonPropertyName.ToLowerInvariant();

        propertySchema.Description ??= (typeName, name) switch
        {
            ("LoginRequest", "schoolid") => "Required school ID used as the account identifier.",
            ("LoginRequest", "password") => "Required account password.",

            ("RegisterRequest", "firstname") => "Required first name for the account request.",
            ("RegisterRequest", "lastname") => "Required last name for the account request.",
            ("RegisterRequest", "email") => "Required valid email address. Must be unique.",
            ("RegisterRequest", "schoolid") => "Required institutional School ID. Students use 10 digits; faculty and department heads use 5 digits.",
            ("RegisterRequest", "role") => "Required requested role. Allowed self-registration roles are DepartmentHead, Educator, and Student.",
            ("RegisterRequest", "departmentname") => "Required engineering department name.",
            ("RegisterRequest", "password") => "Required password, at least 8 characters with an uppercase letter, number, and special character.",
            ("RegisterRequest", "confirmpassword") => "Required confirmation value. Must match password.",

            ("ResetPasswordRequest", "email") => "Required email address for the account whose password will be changed.",
            ("ResetPasswordRequest", "newpassword") => "Required new password, at least 8 characters with an uppercase letter, number, and special character.",
            ("ResetPasswordRequest", "confirmpassword") => "Required confirmation value. Must match newPassword.",
            ("RefreshTokenRequest", "refreshtoken") => "Required refresh token value.",
            ("ForgotPasswordRequest", "email") => "Required email address for the password reset request.",

            ("CreateSyllabusRequest", "coursecode") => "Required course or subject code for the syllabus.",
            ("CreateSyllabusRequest", "coursetitle") => "Required course or subject title for the syllabus.",
            ("CreateSyllabusRequest", "academicyear") => "Required academic year. The handler defaults blank values to 2025-2026.",
            ("CreateSyllabusRequest", "semester") => "Required semester. The handler defaults blank values to First Semester.",
            ("CreateSyllabusRequest", "instructorid") => "Optional instructor School ID. Defaults to the authenticated user when omitted or blank.",
            ("CreateSyllabusRequest", "filename") => "Optional stored/display file name. Defaults to a course-code syllabus file name.",
            ("CreateSyllabusRequest", "changesummary") => "Optional change summary. Currently accepted for metadata compatibility.",

            ("UploadSyllabusFormRequest", "file") => "Required syllabus document file uploaded as multipart/form-data. Empty files are rejected.",
            ("UploadSyllabusFormRequest", "coursecode") => "Required course or subject code for the uploaded syllabus.",
            ("UploadSyllabusFormRequest", "coursetitle") => "Required course or subject title for the uploaded syllabus.",
            ("UploadSyllabusFormRequest", "academicyear") => "Optional academic year. Defaults to 2025-2026 when omitted or blank.",
            ("UploadSyllabusFormRequest", "semester") => "Optional semester. Defaults to First Semester when omitted or blank.",
            ("UploadSyllabusFormRequest", "instructorid") => "Optional instructor School ID. Defaults to the authenticated user when omitted or blank.",
            ("UploadSyllabusFormRequest", "changesummary") => "Optional upload change summary. A default summary is used when omitted or blank.",

            ("UpdateSyllabusRequest", "coursecode") => "Optional new course or subject code. Blank values are ignored.",
            ("UpdateSyllabusRequest", "coursetitle") => "Optional new course or subject title. Blank values are ignored.",
            ("UpdateSyllabusRequest", "academicyear") => "Optional new academic year. Blank values are ignored.",
            ("UpdateSyllabusRequest", "semester") => "Optional new semester. Blank values are ignored.",
            ("UpdateSyllabusRequest", "instructorid") => "Optional new instructor School ID. Blank values are ignored.",
            ("UpdateSyllabusRequest", "filename") => "Optional new current file name. Blank values are ignored.",
            ("UpdateSyllabusRequest", "changesummary") => "Optional latest change summary. Blank values are ignored.",

            ("AssignRequest", "studentid") => "Required active student School ID.",
            ("AssignRequest", "syllabusid") => "Required syllabus identifier. Accepts the 5-digit document ID or GUID.",
            ("BulkAssignRequest", "studentids") => "Required list of active student School IDs. At least one ID is required.",
            ("BulkAssignRequest", "syllabusid") => "Required syllabus identifier. Accepts the 5-digit document ID or GUID.",
            ("ReviewSyllabusRequest", "remarks") => "Reviewer remarks. Optional for approval; required for rejection.",

            _ => null
        };
    }

    private static string ToJsonPropertyName(PropertyInfo property)
    {
        var jsonName = property.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>()?.Name;
        if (!string.IsNullOrWhiteSpace(jsonName)) return jsonName;

        return char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
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
            if (operation == null) return;

            var path = NormalizePath(context.ApiDescription.RelativePath);
            var method = context.ApiDescription.HttpMethod?.ToUpperInvariant() ?? string.Empty;

            EnsureRequestBodyDocumentation(operation, context, path, method);

            if (operation.Parameters == null) return;

            foreach (var parameter in operation.Parameters)
            {
                if (parameter == null) continue;

                ApplyParameterDocumentation(parameter, path);
            }
        }
        catch
        {
            // Ignore filter errors to prevent OpenAPI generator crashes
        }
    }

    private static void ApplyParameterDocumentation(OpenApiParameter parameter, string path)
    {
        var name = parameter.Name?.ToLowerInvariant();

        if (parameter.In == ParameterLocation.Path)
        {
            parameter.Required = true;
            parameter.Schema.Type = "string";

            switch (name)
            {
                case "id" when path.Contains("/api/admin/users/{id}"):
                    parameter.Description = "Required target user School ID.";
                    parameter.Example = new OpenApiString("10001");
                    return;
                case "id" when path.Contains("/api/admin/registrations/{id}"):
                    parameter.Description = "Required registration record ID, which is the user's School ID.";
                    parameter.Example = new OpenApiString("10001");
                    return;
                case "id" when path.Contains("/api/faculty/syllabi/{id}"):
                    parameter.Description = "Required syllabus identifier owned by the authenticated faculty member. Accepts the 5-digit document ID or GUID.";
                    parameter.Example = new OpenApiString("12345");
                    return;
                case "syllabusid":
                    parameter.Description = "Required syllabus identifier in the department. Accepts the 5-digit document ID or GUID.";
                    parameter.Example = new OpenApiString("12345");
                    return;
                case "syllabusdocumentid":
                    parameter.Description = "Required syllabus document identifier. Accepts the 5-digit document ID or GUID.";
                    parameter.Example = new OpenApiString("12345");
                    return;
                case "versionid":
                    parameter.Description = "Required syllabus version identifier. Current implementation resolves this as a syllabus document 5-digit ID or GUID.";
                    parameter.Example = new OpenApiString("12345");
                    return;
            }
        }

        switch (name)
        {
            case "status":
                parameter.Required = false;
                parameter.Description = "Optional syllabus status filter. Values: 0 = Draft, 1 = Submitted, 2 = Approved, 3 = Rejected.";
                parameter.Example = new OpenApiInteger(1);
                break;
            case "search":
                parameter.Required = false;
                parameter.Description = "Optional search keyword for pending registrations. Matches full name, email, or School ID.";
                parameter.Example = new OpenApiString("Jane");
                break;
            case "term":
                parameter.Required = false;
                parameter.Description = "Optional search keyword for accessible syllabus course code or title.";
                parameter.Example = new OpenApiString("CPE");
                break;
            case "maxresults":
                parameter.Required = false;
                parameter.Description = "Optional maximum number of syllabus search results. Values less than or equal to 0 are treated as 100.";
                parameter.Example = new OpenApiInteger(10);
                break;
        }
    }

    private static void EnsureRequestBodyDocumentation(OpenApiOperation operation, OperationFilterContext context, string path, string method)
    {
        operation.RequestBody ??= CreateRequestBodyForEndpoint(context, path, method);
        if (operation.RequestBody == null) return;

        operation.RequestBody.Description = (method, path) switch
        {
            ("POST", "/api/auth/register") => "Required JSON body containing self-registration details.",
            ("POST", "/api/auth/login") => "Required JSON body containing school ID and password.",
            ("POST", "/api/auth/reset-password") => "Required JSON body containing email and new password values.",
            ("POST", "/api/auth/refresh-token") => "Required JSON body containing the refresh token value.",
            ("POST", "/api/auth/forgot-password") => "Required JSON body containing the account email address.",

            ("POST", "/api/faculty/syllabi") => "Required JSON body containing syllabus metadata.",
            ("PUT", var p) when p == "/api/faculty/syllabi/{id}" => "Required JSON body containing only the syllabus metadata fields to update.",
            ("POST", "/api/faculty/syllabi/upload") => "Required multipart/form-data body containing the syllabus file and metadata.",

            ("POST", "/api/depthead/syllabi") => "Required JSON body containing syllabus metadata.",
            ("POST", "/api/depthead/syllabi/upload") => "Required multipart/form-data body containing the syllabus file and metadata.",
            ("PUT", "/api/depthead/syllabi/{syllabusid}/approve") => "Optional JSON body. When provided, only remarks is read.",
            ("PUT", "/api/depthead/syllabi/{syllabusid}/reject") => "Required JSON body containing reviewer remarks.",
            ("POST", "/api/depthead/assign") => "Required JSON body containing studentId and syllabusId.",
            ("POST", "/api/depthead/assign/bulk") => "Required JSON body containing studentIds and syllabusId.",
            _ => operation.RequestBody.Description
        };

        operation.RequestBody.Required = path switch
        {
            "/api/depthead/syllabi/{syllabusid}/approve" => false,
            _ => true
        };

        if (path == "/api/admin/registrations/{id}/reject")
        {
            operation.RequestBody = null;
            return;
        }

        if (path == "/api/depthead/syllabi/{syllabusid}/approve")
        {
            operation.RequestBody.Required = false;
        }

        if (path == "/api/depthead/syllabi/{syllabusid}/reject")
        {
            operation.RequestBody.Required = true;
            InlineReviewRemarksBody(operation);
        }

        if (path == "/api/depthead/assign")
        {
            MarkRequestPropertyRequired(operation, context, "studentId", "syllabusId");
        }

        if (path == "/api/depthead/assign/bulk")
        {
            MarkRequestPropertyRequired(operation, context, "studentIds", "syllabusId");
        }
    }

    private static OpenApiRequestBody? CreateRequestBodyForEndpoint(OperationFilterContext context, string path, string method)
    {
        return (method, path) switch
        {
            ("POST", "/api/auth/register") => CreateJsonBody(context, typeof(RegisterRequest)),
            ("POST", "/api/auth/login") => CreateJsonBody(context, typeof(LoginRequest)),
            ("POST", "/api/auth/reset-password") => CreateJsonBody(context, typeof(ResetPasswordRequest)),
            ("POST", "/api/auth/refresh-token") => CreateJsonBody(context, typeof(RefreshTokenRequest)),
            ("POST", "/api/auth/forgot-password") => CreateJsonBody(context, typeof(ForgotPasswordRequest)),

            ("POST", "/api/faculty/syllabi") => CreateJsonBody(context, typeof(CreateSyllabusRequest)),
            ("PUT", "/api/faculty/syllabi/{id}") => CreateJsonBody(context, typeof(UpdateSyllabusRequest)),
            ("POST", "/api/faculty/syllabi/upload") => CreateMultipartBody(context, typeof(UploadSyllabusFormRequest)),

            ("POST", "/api/depthead/syllabi") => CreateJsonBody(context, typeof(CreateSyllabusRequest)),
            ("POST", "/api/depthead/syllabi/upload") => CreateMultipartBody(context, typeof(UploadSyllabusFormRequest)),
            ("PUT", "/api/depthead/syllabi/{syllabusid}/approve") => CreateJsonBody(context, typeof(ReviewSyllabusRequest), required: false),
            ("PUT", "/api/depthead/syllabi/{syllabusid}/reject") => CreateJsonBody(context, typeof(ReviewSyllabusRequest)),
            ("POST", "/api/depthead/assign") => CreateJsonBody(context, typeof(AssignRequest)),
            ("POST", "/api/depthead/assign/bulk") => CreateJsonBody(context, typeof(BulkAssignRequest)),
            _ => null
        };
    }

    private static OpenApiRequestBody CreateJsonBody(OperationFilterContext context, Type bodyType, bool required = true)
    {
        return new OpenApiRequestBody
        {
            Required = required,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = context.SchemaGenerator.GenerateSchema(bodyType, context.SchemaRepository)
                }
            }
        };
    }

    private static OpenApiRequestBody CreateMultipartBody(OperationFilterContext context, Type bodyType)
    {
        return new OpenApiRequestBody
        {
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = context.SchemaGenerator.GenerateSchema(bodyType, context.SchemaRepository)
                }
            }
        };
    }

    private static void MarkRequestPropertyRequired(OpenApiOperation operation, OperationFilterContext context, params string[] propertyNames)
    {
        foreach (var mediaType in operation.RequestBody?.Content.Values ?? Enumerable.Empty<OpenApiMediaType>())
        {
            var schema = mediaType.Schema;
            if (schema == null) continue;
            if (schema.Reference != null &&
                !string.IsNullOrWhiteSpace(schema.Reference.Id) &&
                context.SchemaRepository.Schemas.TryGetValue(schema.Reference.Id, out var referencedSchema))
            {
                schema = referencedSchema;
            }

            foreach (var propertyName in propertyNames)
            {
                schema.Required.Add(propertyName);
            }
        }
    }

    private static void InlineReviewRemarksBody(OpenApiOperation operation)
    {
        foreach (var mediaType in operation.RequestBody?.Content.Values ?? Enumerable.Empty<OpenApiMediaType>())
        {
            mediaType.Schema = new OpenApiSchema
            {
                Type = "object",
                Required = new SortedSet<string> { "remarks" },
                Properties =
                {
                    ["remarks"] = new OpenApiSchema
                    {
                        Type = "string",
                        Nullable = false,
                        Description = "Required reviewer feedback explaining why the syllabus is rejected.",
                        Example = new OpenApiString("Please revise the course outcomes and assessment mapping.")
                    }
                }
            };
        }
    }

    private static string NormalizePath(string? path)
    {
        var normalized = "/" + (path ?? string.Empty).Trim('/');
        return normalized.ToLowerInvariant();
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
