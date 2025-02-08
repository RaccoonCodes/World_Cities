# WorldCities

# Table of Contents
- [WorldCities](#worldcities)
- [Packages](#packages)
- [Back-End](#back-end)
  - [JWT Handler](#jwt-handler)
  - [ApplicationDbContext](#applicationdbcontext)
- [Front-End](#front-end)
  - [Countries Component](#countries-component)
  - [BaseFormComponent](#baseformcomponent)
- [Conclusion](#conclusion)


This a small project that uses an excel spreadsheet containing worldcities. The excel data was obtain via: `https://simplemaps.com/data/world-cities`

The purpose of this project is to demostarte the usage and communication between Angular and .NET core 8. This Repository contains:

Front-end: `Angular 19` 

Back-end: `.NET 8`  

Unit testing: `Moq` and `Xunit`

**Note**: The database is hidden so you must include your own connection for this project to work. You can either add the connection in `My Secrets` or in `appsettings.json`. Afterwords, you must run the migrations and seed controller to populate necessary data and table for the website to use. otherwise a blank site will appear.

## Packages
The following packages where used for back end:

`Microsoft.EntityFrameworkCore version 8.0.0`

`Microsoft.EntityFrameworkCore.Tools version 8.0.0`

`Microsoft.EntityFrameworkCore.SqlServer version 8.0.0`

`Microsoft.AspNetCore.Identity.EntityFrameworkCore version 8.0.0`

`Microsoft.AspNetCore.Authentication.JwtBearer version 8.0.0`

`EPPlus version 4.5.3.3 `

`EFCore.BulkExtensions version 8.0.1`

`Serilog.AspNetCore version 9.0.0`

`Serilog.Settings.Configuration version 9.0.0`

`Serilog.Sinks.MSSqlServer version 8.1.0`

`HotChocolate.AspNetCore version 13.7.0`

`HotChocolate.AspNetCore.Authorization version 13.7.0`

`HotChocolate.Data.EntityFramework version 13.7.0`

Unit Testing Packages:

`Moq version 4.20.70`

`Microsoft.EntityFrameworkCore.InMemory version 8.0.11`


The classes and models are pretty staright forward, so I will focus on a few data for back end and move into front-end work

## Back-End

### JWT Handler
This class is responsible for generating JWT for user authentication. 

```csharp
private readonly IConfiguration _configuration;
private readonly UserManager<ApplicationUser> _userManager;

public JwtHandler(IConfiguration configuration, UserManager<ApplicationUser> userManager)
    => (_configuration, _userManager) = (configuration, userManager);

```
It uses Dependency Injection to initialize these dependency to manage JWT setting from appsettings.json and user authentication. 

```csharp
public async Task<JwtSecurityToken> GetTokenAsync(ApplicationUser user)
{
    var jwt = new JwtSecurityToken(
        issuer: _configuration["JwtSettings:Issuer"],
        audience: _configuration["JwtSettings:Audience"],
        claims: await GetClaimsAsync(user),
        expires: DateTime.Now.AddMinutes(Convert.ToDouble(
            _configuration["JwtSettings:ExpirationTimeInMinutes"])),
        signingCredentials: GetSigningCredentials());

    return jwt;
}

```
This method retrieves JWT from the configuration and calls `GetClaimsAsync(user)` method that fetches user's claims (explanation of the method below). This function also set an expiration time for the token when created and calls `GetSigningCredentials()` method to generate signature for the token. Once created, it returns the `JwtSecurityToken ` object type.

```csharp
private SigningCredentials GetSigningCredentials()
{
    var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecurityKey"]!);
    var secret = new SymmetricSecurityKey(key);
    return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
}
```
The security key is retrived from configuration and creates a `SymmetricSecurityKey`, which is a secure key for signing JWTs and returns a `SigningCredentials`

```csharp
private async Task<List<Claim>> GetClaimsAsync(ApplicationUser user)
{
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Email!)
    };

    foreach (var role in await _userManager.GetRolesAsync(user))
    {
        claims.Add(new Claim(ClaimTypes.Role, role));
    }

    return claims;
}
```
This method Creates a list of claims and Adds the user’s email as a Name claim.
Afterwords, it returns `Claim` object.

### ApplicationDbContext
This database context handles EF Core and extends to Identity for user authentication.

```csharp
public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
{
}
```
`DbContextOptions<ApplicationDbContext>` is injected via Dependency Injection and it passes `options` to the base `IdentityDbContext`, which allows the method to connect to the database. 

```csharp
public DbSet<City> Cities { get; set; }
public DbSet<Country> Countries { get; set; }
```
EF Core maps these classes to their respective database tables.

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
}
```
The `modelBuilder.ApplyConfigurationsFromAssembly` Automatically applies entity configurations from all classes implementing IEntityTypeConfiguration<T>, which are in 
`CityEntityTypeConfiguration` class and `CountryEntityTypeConfiguration` class. This way it organizes entities and separates them into different files

## Front End 

### Countries Component
This Component is responsible for  displaying a paginated, sortable, and filterable list of countries using Angular Material's table : `MatTableDataSource`.

**Properties Component**
```typescript
public displayedColumns: string[] = ['id', 'name', 'iso2', 'iso3', 'totCities'];
public countries!: MatTableDataSource<Country>;
defaultPageIndex: number = 0;
defaultPageSize: number = 10;
public defaultSortColumn: string = "name";
public defaultSortOrder: "asc" | "desc" = "asc";
defaultFilterColumn: string = "name";
filterQuery?: string;

```
`displayedColumns` : Defines which columns appear in the table.

`countries`:Holds the data source (MatTableDataSource) for the table.

`Sorting Defaults`: Sorts by "name" column in ascending (asc) order.

`Filtering Defaults`: Filters based on the "name" column

```typescript
filterTextChanged: Subject<string> = new Subject<string>();

onFilterTextChanged(filterText: string) {
  if (!this.filterTextChanged.observed) {
    this.filterTextChanged
      .pipe(debounceTime(1000), distinctUntilChanged())
      .subscribe(query => {
        this.loadData(query);
      });
  }
  this.filterTextChanged.next(filterText);
}
```
This function debounces API calls by waiting for the user to finish typing before making a request. it does so by waiting 1 second `(debounceTime(1000))` before executing the API call and Ignores duplicate requests `(distinctUntilChanged())`.

```typescript
loadData(query?: string) {
  var pageEvent = new PageEvent();
  pageEvent.pageIndex = this.defaultPageIndex;
  pageEvent.pageSize = this.defaultPageSize;
  this.filterQuery = query;
  this.getData(pageEvent);
}
```
Initilizes `PageEvent` with default page size and index declared in Properties Component. Stores the `filterQuery` from the search input and Calls `getData(pageEvent)` to fetch the data.

**Fetching Data from API**
```typescript
getData(event: PageEvent) {
  var sortColumn = (this.sort)
    ? this.sort.active
    : this.defaultSortColumn;
  var sortOrder = (this.sort)
    ? this.sort.direction
    : this.defaultSortOrder;
  var filterColumn = (this.filterQuery)
    ? this.defaultFilterColumn
    : null;
  var filterQuery = (this.filterQuery)
    ? this.filterQuery
    : null;

  this.countryService.getData(
    event.pageIndex,
    event.pageSize,
    sortColumn,
    sortOrder,
    filterColumn,
    filterQuery)
    .subscribe({
      next: (result) => {
        this.paginator.length = result.totalCount;
        this.paginator.pageIndex = result.pageIndex;
        this.paginator.pageSize = result.pageSize;
        this.countries = new MatTableDataSource<Country>(result.data);
      },
      error: (error) => console.error(error)
    });
}
```
The sorting sections applies sorting selection if applied, otherwise it falls back to default sorting selection. The filtering following the same principle as sorting sections.

the getData function sends pagination, sorting, and filtering parameters and `Subscribes` to the response: Updates the paginator and Sets countries to a new table data source.

**Cities**
The Cities follows the same method and functionality as Countries.

### BaseFormComponent
The BaseFormComponent is an abstract class that serves as a base component for managing forms. It handles validation messages, making form management more reusable across different form components.

```typescript
export abstract class BaseFormComponent {
  // the form model
  form!: FormGroup;

  getErrors(
    control: AbstractControl,
    displayName: string,
    customMessages: { [key: string]: string } | null = null
  ): string[] {
    var errors: string[] = [];
    Object.keys(control.errors || {}).forEach((key) => {
      switch (key) {
        case 'required':
          errors.push(`${displayName} ${customMessages?.[key] ?? "is required."}`);
          break;
        case 'pattern':
          errors.push(`${displayName} ${customMessages?.[key] ?? "contains invalid characters."}`);
          break;
        case 'isDupeField':
          errors.push(`${displayName} ${customMessages?.[key] ?? "already exists: please choose another."}`);
          break;
        default:
          errors.push(`${displayName} is invalid.`);
          break;
      }
    });
    return errors;
  }
  constructor() { }
}
```
`form!: FormGroup;`: This is meant to be initialized in derived components that extend this base class.

`getErrors` function: accepts 3 data types, `AbstractControl`, string, and customMessages.

The loops goes through validation errors in `Object.keys(control.errors || {})`.
The switch statement generates appropiate error message based on the type of error that is being produced. 

Since this is an abstract component, it can extent to other component so that other component can use this validation class  such as `city-edit.component.ts`.

## Conclusion
This mini project is to demostrate the use of Angular 19 and .NET core 8. Although it is currently not much, in the future, I can continue to develop and improve using Angular as my front-end as I have an extensive knowledge in C# and C++.


