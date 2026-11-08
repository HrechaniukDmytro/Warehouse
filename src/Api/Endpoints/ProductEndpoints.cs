using System.Security.Cryptography;
using Api.Contracts;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints;

public static class ProductEndpoints
{
    public static void MapProducts(this WebApplication app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        // GET /api/products
        group.MapGet("/", (ICatalogStore store) =>
            TypedResults.Ok(store.List().Select(p => p.ToDto()).ToList()));

        group.MapGet("/low-stock", (ICatalogStore store, int threshold = 5) =>
            TypedResults.Ok(store.Find(p => p.Quantity <= threshold).Select(p => p.ToDto()).ToList()));

        // GET /api/products/{id}
        group.MapGet("/{id}", Results<Ok<ProductDto>, NotFound>
            (string id, ICatalogStore store) =>
            store.GetById(id) is { } found
                ? TypedResults.Ok(found.ToDto())
                : TypedResults.NotFound());

        // POST /api/products
        group.MapPost("/", Results<Created<ProductDto>, ValidationProblem>
            (SaveProductRequest request, ICatalogStore store) =>
        {
            if (!request.TryValidate(out var errors))
                return TypedResults.ValidationProblem(errors);

            var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();
            var created = Product.Create(id, request.Sku, request.Name, request.Unit, request.Quantity);
            store.Add(created);

            return TypedResults.Created($"/api/products/{created.Id}", created.ToDto());
        });

        // PUT /api/products/{id}
        group.MapPut("/{id}", Results<Ok<ProductDto>, NotFound, ValidationProblem>
            (string id, SaveProductRequest request, ICatalogStore store) =>
        {
            if (!request.TryValidate(out var errors))
                return TypedResults.ValidationProblem(errors);

            if (store.GetById(id) is null)
                return TypedResults.NotFound();

            var replacement = Product.Create(id, request.Sku, request.Name, request.Unit, request.Quantity);
            store.Update(replacement);

            return TypedResults.Ok(replacement.ToDto());
        });

        // DELETE /api/products/{id}
        group.MapDelete("/{id}", Results<NoContent, NotFound>
            (string id, ICatalogStore store) =>
            store.Remove(id)
                ? TypedResults.NoContent()
                : TypedResults.NotFound());
    }
}