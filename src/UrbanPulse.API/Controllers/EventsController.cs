using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Mvc;
using UrbanPulse.Shared;

namespace UrbanPulse.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly ElasticsearchClient _elasticClient;

        public EventsController(ElasticsearchClient elasticClient)
        {
            _elasticClient = elasticClient;
        }

        [HttpGet]
        public async Task<IActionResult> Getall()
        {
            var response = await _elasticClient.SearchAsync<UrbanEvent>(s => s
                .Indices("urban-events")
                .Size(50)
                .Sort(sort => sort.Field(f => f.Timestamp, d => d.Order(SortOrder.Desc)))
            );

            if(!response.IsSuccess())
            {
                return BadRequest(response.DebugInformation);
            }

            return Ok(response.Documents);
        }

        [HttpGet("proximity")]
        public async Task<IActionResult> GetByProximity(double lat, double lon, double radiusKm = 5)
        {
            var response = await _elasticClient.SearchAsync<UrbanEvent>(s => s
                .Indices("urban-events")
                .Query(q => q
                    .GeoDistance(g => g
                        .Field(f => f.Location)
                        .Distance($"{radiusKm}km")
                        .Location(new Elastic.Clients.Elasticsearch.LatLonGeoLocation { Lat = lat, Lon = lon })
                    )
                )
                .Sort(sort => sort.Field(f => f.Timestamp, d => d.Order(SortOrder.Desc)))
            );

            if (!response.IsSuccess())
            {
                return BadRequest(response.DebugInformation);
            }

            return Ok(new
            {
                Total = response.Documents.Count,
                Radius = $"{radiusKm}km",
                Events = response.Documents
            });
        }
    }
}
