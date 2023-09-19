using App;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

IConfigurationRoot builder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

string connectionString = builder.GetConnectionString("DefaultConnection") ?? "";
var optionsBuiler = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlServer(connectionString);

optionsBuiler.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));

AppDbContext appDbContext = new(optionsBuiler.Options);

_ = await appDbContext.Database.EnsureDeletedAsync();
_ = await appDbContext.Database.EnsureCreatedAsync();

SeedDbHandler seed = new(appDbContext);
await seed.SeedDb();

var clientsWithThree = await appDbContext.Clients
    .Include(x => x.Projects)
    .Where(x => x.Projects.Any(x => x.Name.Contains("3")))
    .ToListAsync();

//
clientsWithThree.ForEach(x => {
    Console.WriteLine(x.Name);
    x.Projects.ForEach(x => Console.WriteLine($"  {x.Name}"));
});

namespace App {

    public class AppDbContext : DbContext {
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }


        // OnModelCreating
        protected override void OnModelCreating(ModelBuilder builder) {

            builder.Entity<Client>(p => {
                p.ToTable("Client");
                p.HasKey(x => x.Id);
                p.Property(x => x.Id).ValueGeneratedOnAdd();
                p.Property(x => x.Name);
                p.HasIndex(x => x.Name).IsUnique();
            });

            builder.Entity<Project>(p => {
                p.ToTable("Project");
                p.HasKey(x => x.Id);
                p.Property(x => x.Id).ValueGeneratedOnAdd();
                p.Property(x => x.Name);
                p.HasIndex(x => new { x.ClientId, x.Name }).IsUnique();
            });
        }
    }

    public class Client {

        public Guid Id { get; set; }
        public string Name { get; set; } = null!;

        public List<Project> Projects { get; set; } = new();
    }

    public class Project {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;

        public Guid ClientId { get; set; }
        public Client Client { get; set; } = null!;
    }

    public class User {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public List<Client> Clients { get; set; } = new();
    }


    // --
    public class SeedDbHandler {
        private readonly AppDbContext _dbContext;

        // appdbcontext
        public SeedDbHandler(AppDbContext dbContext) {
            _dbContext = dbContext;
        }
        public async Task<SeedResultDTO> SeedDb() {
            List<Client> clients = new() {
                new Client { Name = "Client 1",
                    Projects = new List<Project> {
                        new Project { Name = "Client1 proj 1" },
                        new Project { Name = "Client1 proj 2" },
                        new Project { Name = "Client1 proj 3" }
                    }
                 },
                new Client { Name = "Client 2",
                    Projects = new List<Project> {
                        new Project { Name = "Project 1" },
                        new Project { Name = "Project 2" },
                        new Project { Name = "Project 3" }
                    }
                },
                new Client { Name = "Client 333" }
            };

            _dbContext.Clients.AddRange(clients);

            await _dbContext.SaveChangesAsync();

            SeedResultDTO result = new() {
                Clients = clients.Count,
                Users = 0,
                Projects = 0
            };
            return result;
        }
    }

    public record SeedResultDTO {
        public int Clients { get; set; }
        public int Users { get; set; }
        public int Projects { get; set; }
    }
}




