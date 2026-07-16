using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services;

public class AzureCosmosDbService : IAzureCosmosDbService, IAsyncInitialization
{
    private readonly CosmosClient _cosmosClient;
    private readonly string _databaseName;
    private readonly string _studentContainerName;
    private readonly string _userContainerName;
    private Container? _studentContainer;
    private Container? _userContainer;
    private readonly ILogger<AzureCosmosDbService> _logger;
    private bool _initialized = false;

    public AzureCosmosDbService(IConfiguration configuration, ILogger<AzureCosmosDbService> logger)
    {
        _logger = logger;

        var connectionString = configuration["CosmosDb:ConnectionString"]
            ?? throw new ArgumentNullException("CosmosDb:ConnectionString");
        _databaseName = configuration["CosmosDb:DatabaseName"]
            ?? throw new ArgumentNullException("CosmosDb:DatabaseName");
        _studentContainerName = configuration["CosmosDb:StudentContainer"]
            ?? throw new ArgumentNullException("CosmosDb:StudentContainer");
        _userContainerName = configuration["CosmosDb:UserContainer"]
            ?? throw new ArgumentNullException("CosmosDb:UserContainer");

        _logger.LogInformation("Cosmos DB Configuration:");
        _logger.LogInformation("  Database Name: {DatabaseName}", _databaseName);
        _logger.LogInformation("  Student Container: {StudentContainer}", _studentContainerName);
        _logger.LogInformation("  User Container: {UserContainer}", _userContainerName);

        var options = new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            RequestTimeout = TimeSpan.FromSeconds(30),
            MaxRetryAttemptsOnRateLimitedRequests = 3,
            MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(10),
            Serializer = new CosmosJsonDotNetSerializer()
        };

        _cosmosClient = new CosmosClient(connectionString, options);
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        try
        {
            _logger.LogInformation("Initializing Cosmos DB resources...");

            var account = await _cosmosClient.ReadAccountAsync();
            _logger.LogInformation("Connected to Cosmos DB account: {AccountId}", account.Id);

            _logger.LogInformation("Ensuring database exists: {DatabaseName}", _databaseName);
            var databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseName);
            _logger.LogInformation("Database ready: {DatabaseId}, Status: {StatusCode}",
                databaseResponse.Database.Id, databaseResponse.StatusCode);

            _logger.LogInformation("Ensuring container exists: {ContainerName}", _studentContainerName);
            var studentContainerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync(
                new ContainerProperties(_studentContainerName, partitionKeyPath: "/id")
                {
                    DefaultTimeToLive = -1
                });
            _studentContainer = studentContainerResponse.Container;
            _logger.LogInformation("Student container ready: {ContainerId}", studentContainerResponse.Container.Id);

            _logger.LogInformation("Ensuring container exists: {ContainerName}", _userContainerName);
            var userContainerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync(
                new ContainerProperties(_userContainerName, partitionKeyPath: "/id")
                {
                    DefaultTimeToLive = -1
                });
            _userContainer = userContainerResponse.Container;
            _logger.LogInformation("User container ready: {ContainerId}", userContainerResponse.Container.Id);

            _initialized = true;
            _logger.LogInformation("Cosmos DB initialization completed successfully");
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogError(ex, "Cosmos DB authorization failed");
            throw new InvalidOperationException("Failed to connect to Cosmos DB: Invalid credentials", ex);
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Cosmos DB error: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
            throw new InvalidOperationException($"Cosmos DB error: {ex.StatusCode}", ex);
        }
    }

    private Container StudentContainer => _studentContainer ?? throw new InvalidOperationException("Service not initialized");
    private Container UserContainer => _userContainer ?? throw new InvalidOperationException("Service not initialized");

    public async Task<Student?> GetStudentAsync(string id)
    {
        try
        {
            var response = await StudentContainer.ReadItemAsync<Student>(id, new PartitionKey(id));
            return response.Resource?.IsDeleted == false ? response.Resource : null;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IEnumerable<Student>> GetStudentsAsync(int page = 1, int pageSize = 10)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE (NOT IS_DEFINED(c.isDeleted) OR c.isDeleted = false)");
        var iterator = StudentContainer.GetItemQueryIterator<Student>(query);

        var allStudents = new List<Student>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            allStudents.AddRange(response);
        }

        _logger.LogInformation("GetStudentsAsync retrieved {Count} students from database", allStudents.Count);

        var sortedStudents = allStudents
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        _logger.LogInformation("GetStudentsAsync returning {Count} students after sorting and pagination", sortedStudents.Count);

        return sortedStudents;
    }

    public async Task<int> GetTotalStudentCountAsync()
    {
        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE (NOT IS_DEFINED(c.isDeleted) OR c.isDeleted = false)");
        var iterator = StudentContainer.GetItemQueryIterator<int>(query);

        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            var count = response.FirstOrDefault();
            _logger.LogInformation("GetTotalStudentCountAsync: {Count} total students", count);
            return count;
        }
        return 0;
    }

    public async Task<IEnumerable<Student>> SearchStudentsAsync(string searchTerm, int page = 1, int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetStudentsAsync(page, pageSize);

        searchTerm = searchTerm.ToLowerInvariant();

        var sql = @"SELECT * FROM c 
                    WHERE (NOT IS_DEFINED(c.isDeleted) OR c.isDeleted = false) 
                    AND (CONTAINS(LOWER(c.firstName), @searchTerm) 
                         OR CONTAINS(LOWER(c.lastName), @searchTerm) 
                         OR CONTAINS(c.id, @searchTerm) 
                         OR CONTAINS(LOWER(c.email), @searchTerm))";

        var query = new QueryDefinition(sql).WithParameter("@searchTerm", searchTerm);
        var iterator = StudentContainer.GetItemQueryIterator<Student>(query);

        var allStudents = new List<Student>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            allStudents.AddRange(response);
        }

        _logger.LogInformation("SearchStudentsAsync found {Count} students for term '{SearchTerm}'", allStudents.Count, searchTerm);

        return allStudents
            .OrderBy(s => s.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public async Task<Student> CreateStudentAsync(Student student)
    {
        student.Id = Guid.NewGuid().ToString();
        student.CreatedAt = DateTime.UtcNow;
        student.UpdatedAt = DateTime.UtcNow;
        student.IsDeleted = false;

        _logger.LogInformation("Creating student with ID: {Id}, Name: {FirstName} {LastName}, Email: {Email}",
            student.Id, student.FirstName, student.LastName, student.Email);

        var response = await StudentContainer.CreateItemAsync(student, new PartitionKey(student.Id));

        _logger.LogInformation("Student created successfully. Response Status: {StatusCode}", response.StatusCode);

        return response.Resource;
    }

    public async Task<Student> UpdateStudentAsync(string id, Student student)
    {
        student.UpdatedAt = DateTime.UtcNow;
        var response = await StudentContainer.ReplaceItemAsync(student, id, new PartitionKey(id));
        return response.Resource;
    }

    public async Task DeleteStudentAsync(string id, bool permanent = false)
    {
        if (permanent)
        {
            await StudentContainer.DeleteItemAsync<Student>(id, new PartitionKey(id));
        }
        else
        {
            var student = await GetStudentAsync(id);
            if (student != null)
            {
                student.IsDeleted = true;
                student.UpdatedAt = DateTime.UtcNow;
                await UpdateStudentAsync(id, student);
            }
        }
    }

    public async Task<bool> StudentExistsAsync(string email)
    {
        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.email = @email AND (NOT IS_DEFINED(c.isDeleted) OR c.isDeleted = false)")
            .WithParameter("@email", email);

        var iterator = StudentContainer.GetItemQueryIterator<int>(query);

        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            return response.FirstOrDefault() > 0;
        }
        return false;
    }

    public async Task<ApplicationUser?> GetUserByProviderAsync(string provider, string providerKey)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.provider = @provider AND c.providerKey = @providerKey AND (NOT IS_DEFINED(c.isActive) OR c.isActive = true)")
            .WithParameter("@provider", provider)
            .WithParameter("@providerKey", providerKey);

        var iterator = UserContainer.GetItemQueryIterator<ApplicationUser>(query);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            return response.FirstOrDefault();
        }
        return null;
    }

    public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.email = @email AND (NOT IS_DEFINED(c.isActive) OR c.isActive = true)")
            .WithParameter("@email", email);

        var iterator = UserContainer.GetItemQueryIterator<ApplicationUser>(query);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            return response.FirstOrDefault();
        }
        return null;
    }

    public async Task<ApplicationUser> CreateUserAsync(ApplicationUser user)
    {
        user.Id = Guid.NewGuid().ToString();
        user.CreatedAt = DateTime.UtcNow;
        user.LastLogin = DateTime.UtcNow;
        user.IsActive = true;

        _logger.LogInformation("Creating user with ID: {Id}, Email: {Email}", user.Id, user.Email);

        var response = await UserContainer.CreateItemAsync(user, new PartitionKey(user.Id));
        return response.Resource;
    }

    public async Task UpdateUserAsync(ApplicationUser user)
    {
        user.LastLogin = DateTime.UtcNow;
        await UserContainer.ReplaceItemAsync(user, user.Id, new PartitionKey(user.Id));
    }

    // ========== ARCHIVE / RECYCLE BIN METHODS ==========

    public async Task<IEnumerable<Student>> GetArchivedStudentsAsync(int page = 1, int pageSize = 10)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.isDeleted = true");
        var iterator = StudentContainer.GetItemQueryIterator<Student>(query);

        var allArchived = new List<Student>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            allArchived.AddRange(response);
        }

        return allArchived
            .OrderByDescending(s => s.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public async Task<int> GetArchivedStudentCountAsync()
    {
        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.isDeleted = true");
        var iterator = StudentContainer.GetItemQueryIterator<int>(query);

        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            return response.FirstOrDefault();
        }
        return 0;
    }

    public async Task<Student?> GetArchivedStudentAsync(string id)
    {
        try
        {
            var response = await StudentContainer.ReadItemAsync<Student>(id, new PartitionKey(id));
            return response.Resource?.IsDeleted == true ? response.Resource : null;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task RestoreStudentAsync(string id)
    {
        var student = await GetArchivedStudentAsync(id);
        if (student != null)
        {
            student.IsDeleted = false;
            student.UpdatedAt = DateTime.UtcNow;
            await UpdateStudentAsync(id, student);
        }
    }

    public async Task<int> EmptyRecycleBinAsync()
    {
        var archived = await GetArchivedStudentsAsync(1, int.MaxValue);
        var count = 0;

        foreach (var student in archived)
        {
            await StudentContainer.DeleteItemAsync<Student>(student.Id, new PartitionKey(student.Id));
            count++;
        }

        return count;
    }

    // ========== ANALYTICS METHODS ==========

    public async Task<IEnumerable<Student>> GetAllStudentsForAnalyticsAsync()
    {
        var query = new QueryDefinition("SELECT * FROM c");
        var iterator = StudentContainer.GetItemQueryIterator<Student>(query);

        var allStudents = new List<Student>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            allStudents.AddRange(response);
        }

        return allStudents;
    }

    public async Task<IEnumerable<ApplicationUser>> GetAllUsersAsync()
    {
        var query = new QueryDefinition("SELECT * FROM c");
        var iterator = UserContainer.GetItemQueryIterator<ApplicationUser>(query);

        var allUsers = new List<ApplicationUser>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            allUsers.AddRange(response);
        }

        return allUsers;
    }
}

public interface IAsyncInitialization
{
    Task InitializeAsync();
}

public class CosmosJsonDotNetSerializer : CosmosSerializer
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    private static readonly JsonSerializer Serializer = JsonSerializer.Create(Settings);

    public override T FromStream<T>(Stream stream)
    {
        using var reader = new StreamReader(stream);
        using var jsonReader = new JsonTextReader(reader);
        return Serializer.Deserialize<T>(jsonReader)!;
    }

    public override Stream ToStream<T>(T input)
    {
        var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, leaveOpen: true))
        using (var jsonWriter = new JsonTextWriter(writer))
        {
            Serializer.Serialize(jsonWriter, input);
        }
        stream.Position = 0;
        return stream;
    }
}