﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldCities.Server.Data;
using WorldCities.Server.Data.Models;
using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Authorization;


namespace WorldCities.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CountriesController(ApplicationDbContext context)
        => _context = context;

        [HttpGet]
        public async Task<ActionResult<ApiResult<CountryDTO>>> GetCountries(
        int pageIndex = 0,
        int pageSize = 10,
        string? sortColumn = null,
        string? sortOrder = null,
        string? filterColumn = null,
        string? filterQuery = null)
        {
            return await ApiResult<CountryDTO>.CreateAsync(
                    _context.Countries.AsNoTracking()
                        .Select(c => new CountryDTO()
                        {
                            Id = c.Id,
                            Name = c.Name,
                            ISO2 = c.ISO2,
                            ISO3 = c.ISO3,
                            TotCities = c.Cities!.Count
                        }),
                    pageIndex,
                    pageSize,
                    sortColumn,
                    sortOrder,
                    filterColumn,
                    filterQuery);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Country>> GetCountry(int id)
        {
            var country = await _context.Countries.FindAsync(id);

            if (country == null)
            {
                return NotFound();
            }

            return country;
        }

        [Authorize(Roles = "RegisteredUser")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCountry(int id, Country country)
        {
            if (id != country.Id)
            {
                return BadRequest();
            }

            _context.Entry(country).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CountryExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        [Authorize(Roles = "RegisteredUser")]
        public async Task<ActionResult<Country>> PostCountry(Country country)
        {
            _context.Countries.Add(country);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCountry", new { id = country.Id }, country);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteCountry(int id)
        {
            var country = await _context.Countries.FindAsync(id);
            if (country == null)
            {
                return NotFound();
            }

            _context.Countries.Remove(country);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CountryExists(int id)
        {
            return _context.Countries.Any(e => e.Id == id);
        }

        [HttpPost]
        [Route("IsDupeField")]
        public bool IsDupeField(int countryId, string fieldName, string fieldValue)
        {
            return ApiResult<Country>
                .IsValidProperty(fieldName, true) && _context.Countries
                .Any(string.Format("{0} == @0 && Id != @1", fieldName), fieldValue, countryId);
        }
    }
}