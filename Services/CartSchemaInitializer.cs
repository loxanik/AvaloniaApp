using System;
using Microsoft.EntityFrameworkCore;
using Shop.Entities;

namespace Shop.Services;

public static class CartSchemaInitializer
{
    public static void EnsureCartTablesCreated(ShopContext context)
    {
        // Проект без миграций: создаем таблицы корзины "на месте", если их нет.
        context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "Cart" (
              id SERIAL PRIMARY KEY,
              "userId" INTEGER NOT NULL,
              "createdAt" TIMESTAMP NOT NULL DEFAULT NOW(),
              "updatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
              CONSTRAINT cart_user_unique UNIQUE ("userId"),
              CONSTRAINT cart_user_id FOREIGN KEY ("userId") REFERENCES "User"(id) ON DELETE CASCADE
            );
            """);

        context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "CartItem" (
              id SERIAL PRIMARY KEY,
              "cartId" INTEGER NOT NULL,
              "productId" INTEGER NOT NULL,
              quantity INTEGER NOT NULL,
              "createdAt" TIMESTAMP NOT NULL DEFAULT NOW(),
              "updatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
              CONSTRAINT cartitem_cart_product_unique UNIQUE ("cartId", "productId"),
              CONSTRAINT cartitem_cart_id FOREIGN KEY ("cartId") REFERENCES "Cart"(id) ON DELETE CASCADE,
              CONSTRAINT cartitem_product_id FOREIGN KEY ("productId") REFERENCES "Product"(id) ON DELETE CASCADE
            );
            """);
    }
}

