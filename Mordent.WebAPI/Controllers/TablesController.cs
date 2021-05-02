using Microsoft.AspNetCore.Mvc;
using Mordent.WebAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Mordent.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TablesController : ControllerBase
    {
        // GET: api/tables
        [HttpGet]
        public IEnumerable<Table> GetTables()
        {
            yield return BuildPeopleTable();
        }

        // GET api/tables/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Table>> GetTable(Guid id)
        {
            var peopleTable = BuildPeopleTable();
            if (id == peopleTable.Id)
                return peopleTable;
            else
                return NotFound();
        }

        private static Table BuildPeopleTable()
        {
            return new Table
            (
                new Guid("2E03A645-D827-4B76-86E4-216CF45FCA6D"),
                "People",
                new List<Field>()
                {
                    new Field(new Guid("489CDB5D-4A34-46A4-ACE5-12EDA475349C"), "Id", "Guid"),
                    new Field(new Guid("80FCEAD2-9B4F-4B3C-8AC8-561620B21FB9"), "Name", "string"),
                    new Field(new Guid("4C51714C-7A8D-4197-81AB-177AB5FF9D7C"), "Age", "int"),
                }
            );
        }

        // POST api/<TablesController>
        [HttpPost]
        public async Task<ActionResult<Table>> Post(Table table)
        {
            // TODO: persist the table metadata;
            // reserve the space in the datafile;
            // handle the transaction.
            return CreatedAtAction(nameof(GetTable), new { id = table.Id }, table);
        }

        // PUT api/<TablesController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTable(Guid id, Table table)
        {
            if (id != table.Id)
                return BadRequest();
            return NoContent();
        }

        // DELETE api/<TablesController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DropTable(Guid id)
        {
            return NoContent();
        }
    }
}
