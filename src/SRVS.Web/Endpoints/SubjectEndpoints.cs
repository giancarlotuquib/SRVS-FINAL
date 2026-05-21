using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SRVS.Web.Data; // ApplicationDbContext resides here
using Microsoft.AspNetCore.Identity;
using SRVS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace SRVS.Web.Endpoints;

public static class SubjectEndpoints
{
    public static void MapSubjectEndpoints(this WebApplication app)
    {
        var subjectGroup = app.MapGroup("/api/subject").WithTags("Subject");

        // GET all subjects
        subjectGroup.MapGet("/", async (ApplicationDbContext db) =>
        {
            var subjects = await db.Set<SRVS.Domain.Entities.Subject>().ToListAsync();
            return Results.Ok(subjects);
        }).WithName("GetAllSubjects");

        // GET subject by id
        subjectGroup.MapGet("/{id}", async (int id, ApplicationDbContext db) =>
        {
            var subj = await db.Set<SRVS.Domain.Entities.Subject>().FindAsync(id);
            return subj is null ? Results.NotFound() : Results.Ok(subj);
        }).WithName("GetSubjectById");

        // POST create subject (placeholder)
        subjectGroup.MapPost("/", async (SRVS.Domain.Entities.Subject subject, ApplicationDbContext db) =>
        {
            db.Add(subject);
            await db.SaveChangesAsync();
            return Results.Created($"/api/subject/{subject.Id}", subject);
        }).WithName("CreateSubject");

        // PUT update subject
        subjectGroup.MapPut("/{id}", async (int id, SRVS.Domain.Entities.Subject updated, ApplicationDbContext db) =>
        {
            var existing = await db.Set<SRVS.Domain.Entities.Subject>().FindAsync(id);
            if (existing is null) return Results.NotFound();
            // Simple property copy (customize as needed)
            existing.Name = updated.Name;
            existing.Code = updated.Code;
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("UpdateSubject");

        // DELETE subject
        subjectGroup.MapDelete("/{id}", async (int id, ApplicationDbContext db) =>
        {
            var existing = await db.Set<SRVS.Domain.Entities.Subject>().FindAsync(id);
            if (existing is null) return Results.NotFound();
            db.Remove(existing);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("DeleteSubject");
    }
}
