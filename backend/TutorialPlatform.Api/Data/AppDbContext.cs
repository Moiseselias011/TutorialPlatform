using Microsoft.EntityFrameworkCore;
using TutorialPlatform.Api.Entities;

namespace TutorialPlatform.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Technology> Technologies => Set<Technology>();
    public DbSet<Tutorial> Tutorials => Set<Tutorial>();
    public DbSet<PersonalList> PersonalLists => Set<PersonalList>();
    public DbSet<SavedTutorial> SavedTutorials => Set<SavedTutorial>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<CommentLike> CommentLikes => Set<CommentLike>();
    public DbSet<Visit> Visits => Set<Visit>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // ---------- Users ----------
        mb.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasDefaultValue(Roles.User);
        });

        // ---------- Technologies ----------
        mb.Entity<Technology>(e =>
        {
            e.HasIndex(t => t.Slug).IsUnique();
            e.HasIndex(t => t.Name).IsUnique();
        });

        // ---------- Tutorials ----------
        mb.Entity<Tutorial>(e =>
        {
            e.HasIndex(t => t.TechnologyId);
            e.HasIndex(t => t.AuthorId);
            e.HasIndex(t => t.PublishedAt);

            e.HasOne(t => t.Technology)
             .WithMany(te => te.Tutorials)
             .HasForeignKey(t => t.TechnologyId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.Author)
             .WithMany(u => u.Tutorials)
             .HasForeignKey(t => t.AuthorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- PersonalLists ----------
        mb.Entity<PersonalList>(e =>
        {
            e.HasIndex(l => new { l.OwnerId, l.Name }).IsUnique();

            e.HasOne(l => l.Owner)
             .WithMany(u => u.PersonalLists)
             .HasForeignKey(l => l.OwnerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- SavedTutorials (pivote N-M) ----------
        mb.Entity<SavedTutorial>(e =>
        {
            e.HasIndex(s => new { s.PersonalListId, s.TutorialId }).IsUnique();

            e.HasOne(s => s.PersonalList)
             .WithMany(l => l.SavedTutorials)
             .HasForeignKey(s => s.PersonalListId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(s => s.Tutorial)
             .WithMany(t => t.SavedInLists)
             .HasForeignKey(s => s.TutorialId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Tecnologías ----------
        mb.Entity<Technology>(e =>
        {
            // Las que ya existían en la BD reciben «programacion» al añadir la
            // columna; sin esto quedarían con la cadena vacía y no aparecerían
            // en ningún índice.
            e.Property(t => t.Domain).HasDefaultValue(Domains.Programacion);
        });

        // ---------- Likes (un usuario = un like por tutorial) ----------
        mb.Entity<Like>(e =>
        {
            e.HasIndex(l => new { l.UserId, l.TutorialId }).IsUnique();

            e.HasOne(l => l.User)
             .WithMany(u => u.Likes)
             .HasForeignKey(l => l.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(l => l.Tutorial)
             .WithMany(t => t.Likes)
             .HasForeignKey(l => l.TutorialId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Comments ----------
        mb.Entity<Comment>(e =>
        {
            e.HasIndex(c => c.TutorialId);
            e.HasIndex(c => c.ParentCommentId);

            e.HasOne(c => c.Tutorial)
             .WithMany(t => t.Comments)
             .HasForeignKey(c => c.TutorialId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(c => c.Author)
             .WithMany(u => u.Comments)
             .HasForeignKey(c => c.AuthorId)
             .OnDelete(DeleteBehavior.Restrict);

            // Respuestas. Restrict (y no Cascade) es deliberado: si el padre se
            // borrara en cascada, las respuestas de otros usuarios morirían con él
            // e infringiríamos §5.3 (nadie elimina contenido que no le pertenece).
            // El controlador promueve antes las respuestas a comentario principal.
            e.HasOne(c => c.Parent)
             .WithMany(p => p.Replies)
             .HasForeignKey(c => c.ParentCommentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- CommentLikes (un usuario = un Me gusta por comentario) ----------
        mb.Entity<CommentLike>(e =>
        {
            e.HasIndex(cl => new { cl.UserId, cl.CommentId }).IsUnique();

            e.HasOne(cl => cl.User)
             .WithMany(u => u.CommentLikes)
             .HasForeignKey(cl => cl.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            // Borrar un comentario borra sus Me gusta: no puede quedar ninguno huérfano.
            e.HasOne(cl => cl.Comment)
             .WithMany(c => c.CommentLikes)
             .HasForeignKey(cl => cl.CommentId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Visits (contador de visitas para el panel del admin) ----------
        mb.Entity<Visit>(e =>
        {
            e.Property(v => v.Path).HasMaxLength(200);
            e.Property(v => v.VisitorHash).HasMaxLength(64);

            // Las dos consultas del resumen: agrupar por fecha y contar
            // visitantes distintos por hash.
            e.HasIndex(v => v.OccurredAt);
            e.HasIndex(v => v.VisitorHash);
        });
    }
}
