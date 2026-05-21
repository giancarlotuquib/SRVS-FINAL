import sys
import os

file_path = r'c:\Users\BSA\OneDrive - BSA Business Solutions\Documents\SRVS\SRVS-SD3\src\SRVS.Web\Program.cs'

with open(file_path, 'r', encoding='utf-8-sig') as f:
    lines = f.readlines()

new_lines = []
skip = False
for i, line in enumerate(lines):
    if line.startswith('using SRVS.Web.Components.Admin.Models;'):
        new_lines.append(line)
        new_lines.append('using SRVS.Web.Endpoints;\n')
        new_lines.append('using Microsoft.OpenApi.Models;\n')
        continue
        
    if line.startswith('builder.Services.AddSwaggerGen();'):
        new_lines.append('builder.Services.AddSwaggerGen(c =>\n')
        new_lines.append('{\n')
        new_lines.append('    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SRVS API", Version = "v1" });\n')
        new_lines.append('});\n')
        continue

    if 'app.MapGet("/syllabi/{syllabusDocumentId:guid}/download"' in line:
        skip = True
        
    if skip:
        if '.WithName("RejectRegistration");' in line:
            skip = False
            new_lines.append('app.MapHealthEndpoints();\n')
            new_lines.append('app.MapSyllabusEndpoints();\n')
            new_lines.append('app.MapRegistrationEndpoints();\n')
            new_lines.append('\n')
            new_lines.append('// Add additional endpoints required by the Identity /Account Razor components.\n')
            new_lines.append('app.MapAdditionalIdentityEndpoints();\n')
        continue
        
    new_lines.append(line)

with open(file_path, 'w', encoding='utf-8-sig') as f:
    f.writelines(new_lines)

print('Update successful')
