using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SRVS.Web.Data;
using Microsoft.AspNetCore.Identity;
using SRVS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace SRVS.Web.Endpoints;

public static class CoursesEndpoints
{
    public static void MapCoursesEndpoints(this WebApplication app)
    {
        var coursesGroup = app.MapGroup("/api/courses").WithTags("Courses");

        // GET all courses
        coursesGroup.MapGet("/", async (ApplicationDbContext db) =>
        {
            var courses = await db.Set<SRVS.Domain.Entities.Subject>().ToListAsync();
            return Results.Ok(courses);
        }).WithName("GetAllCourses");

        // GET course by id
        coursesGroup.MapGet("/{id}", async (int id, ApplicationDbContext db) =>
        {
            var course = await db.Set<SRVS.Domain.Entities.Subject>().FindAsync(id);
            return course is null ? Results.NotFound() : Results.Ok(course);
        }).WithName("GetCourseById");

        // POST create course (placeholder)
        coursesGroup.MapPost("/", async (SRVS.Domain.Entities.Subject course, ApplicationDbContext db) =>
        {
            db.Add(course);
            await db.SaveChangesAsync();
            return Results.Created($"/api/courses/{course.Id}", course);
        }).WithName("CreateCourse");

        // PUT update course
        coursesGroup.MapPut("/{id}", async (int id, SRVS.Domain.Entities.Subject updated, ApplicationDbContext db) =>
        {
            var existing = await db.Set<SRVS.Domain.Entities.Subject>().FindAsync(id);
            if (existing is null) return Results.NotFound();
            existing.Name = updated.Name;
            existing.Code = updated.Code;
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("UpdateCourse");

        // DELETE course
        coursesGroup.MapDelete("/{id}", async (int id, ApplicationDbContext db) =>
        {
            var existing = await db.Set<SRVS.Domain.Entities.Subject>().FindAsync(id);
            if (existing is null) return Results.NotFound();
            db.Remove(existing);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("DeleteCourse");

        // GET course syllabi
        coursesGroup.MapGet("/{id}/syllabi", async (int id, ApplicationDbContext db) =>
        {
            var course = await db.Set<SRVS.Domain.Entities.Subject>().FindAsync(id);
            if (course is null) return Results.NotFound();

            var syllabi = await db.SyllabusDocuments
                .Where(s => s.CourseCode == course.Code)
                .Select(s => new { s.Id, s.CourseCode, s.CourseTitle, s.AcademicYear, s.Semester })
                .ToListAsync();

            return Results.Ok(syllabi);
        }).WithName("GetCourseSyllabi");

        // GET course students
        coursesGroup.MapGet("/{id}/students", async (int id, ApplicationDbContext db) =>
        {
            var course = await db.Set<SRVS.Domain.Entities.Subject>().FindAsync(id);
            if (course is null) return Results.NotFound();

            var students = await db.SyllabusAssignments
                .Join(db.SyllabusDocuments, a => a.SyllabusId, s => s.Id, (a, s) => new { a, s })
                .Where(x => x.s.CourseCode == course.Code && x.a.IsActive)
                .Join(db.Users, x => x.a.StudentId, u => u.Id, (x, u) => new { u.Id, u.FullName, u.InstitutionalId, u.Email })
                .Distinct()
                .ToListAsync();

            return Results.Ok(students);
        }).WithName("GetCourseStudents");
    }
}
