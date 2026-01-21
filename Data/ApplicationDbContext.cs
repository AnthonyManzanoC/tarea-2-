using CoronelExpress.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace CoronelExpress.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<ContactoMessage> ContactoMessages { get; set; }
        public DbSet<QuejaSugerenciaMessage> QuejaSugerenciaMessages { get; set; }
        public DbSet<TrabajaConNosotros> TrabajaConNosotros { get; set; }
        public DbSet<TermsAcceptance> TermsAcceptances { get; set; }
        public DbSet<InventoryMovement> InventoryMovement { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }

        public DbSet<UserProfile> UserProfiles { get; set; } // Agregamos la tabla de perfiles

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Relación entre Product y Category
            builder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
           
            base.OnModelCreating(builder);
            builder.Entity<UserProfile>()
                   .HasOne(p => p.User)
                   .WithOne()
                   .HasForeignKey<UserProfile>(p => p.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        

        // Relación entre OrderDetail y Order
        builder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación entre OrderDetail y Product
            builder.Entity<OrderDetail>()
                .HasOne(od => od.Product)
                .WithMany()
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación entre Order y Customer (actualizada)
            builder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders) // Especifica la propiedad de navegación inversa
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configurar precisión para valores decimales
            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<OrderDetail>()
                .Property(od => od.UnitPrice)
                .HasColumnType("decimal(18,2)");
          
            builder.Entity<Product>()
       .Property(p => p.DiscountPercentage)
       .HasColumnType("decimal(18,2)"); // Define el tipo exacto en la BD

            base.OnModelCreating(builder);
            builder.Entity<Product>()
        .Property(p => p.IsOnOffer)
        .HasColumnName("IsOnOffer");

            base.OnModelCreating(builder);
        }
    }
}
