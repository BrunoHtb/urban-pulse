using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.QueryDsl;    
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
        public async Task<IActionResult> GetAll()
        {
            var response = await _elasticClient.SearchAsync<UrbanEvent>(s => s
                .Indices("urban-events")
                .Size(50)
                .Sort(sort => sort.Field(f => f.Timestamp, d => d.Order(SortOrder.Desc)))
            );
            return response.IsSuccess() ? Ok(response.Documents) : BadRequest(response.DebugInformation);
        }

        [HttpGet("proximity")]
        public async Task<IActionResult> GetByProximity(double lat, double lon, double radiusKm = 5)
        {
            var response = await _elasticClient.SearchAsync<UrbanEvent>(
                new SearchRequest("urban-events")
                {
                    Size = 50,
                    Query = new Query
                    {
                        GeoDistance = new GeoDistanceQuery
                        {
                            Field = "location",
                            Distance = $"{radiusKm}km",
                            Location = new Elastic.Clients.Elasticsearch.LatLonGeoLocation
                            {
                                Lat = lat,
                                Lon = lon
                            }
                        }
                    },
                    Sort = new List<SortOptions>
                    {
                        new SortOptions
                        {
                            Field = new FieldSort
                            {
                                Field = "timestamp",
                                Order = SortOrder.Desc
                            }
                        }
                    }
                }
            );

            if (!response.IsValidResponse)
                return BadRequest(response.DebugInformation);

            return Ok(new
            {
                Total = response.Documents.Count,
                Radius = $"{radiusKm}km",
                Events = response.Documents
            });
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetNeighborhoodStats()
        {
            var queries = new Dictionary<string, Query>
            {
                { "Batel", new MatchQuery { Field = "description", Query = "Batel" } },
                { "Centro", new MatchQuery { Field = "description", Query = "Centro" } },
                { "Linha Verde", new MatchQuery { Field = "description", Query = "Verde" } }
            };

            var response = await _elasticClient.SearchAsync<UrbanEvent>
            (
                new SearchRequest<UrbanEvent>
                {
                    Indices = new[] { "urban-events" },
                    Size = 0,
                    Aggregations = new Dictionary<string, Aggregation>
                    {
                        {
                            "por_bairro",
                            new Aggregation
                            {
                                Filters = new FiltersAggregation
                                {
                                    Filters = queries
                                },

                                Aggregations = new Dictionary<string, Aggregation>
                                {
                                    {
                                        "media_severidade",
                                        new Aggregation
                                        {
                                            Avg = new AverageAggregation
                                            {
                                                Field = "severity"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            );

            if (!response.IsValidResponse)
                return BadRequest(response.DebugInformation);

            var filtersAgg = response.Aggregations?["por_bairro"] as FiltersAggregate;

            var resultado = filtersAgg!.Buckets.Select(bucket =>
            {
                var avgAgg =
                    bucket.Aggregations?["media_severidade"] as AverageAggregate;

                return new
                {
                    Bairro = bucket.Key,
                    NivelCongestionamento = Math.Round(avgAgg?.Value ?? 0, 2),
                    QuantidadeRegistros = bucket.DocCount
                };
            });

            return Ok(resultado);
        }

    }
}
