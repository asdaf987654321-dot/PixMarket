-- ============================================================================
--  PixMarket: catalogo inicial de cartas
-- ----------------------------------------------------------------------------
--  Inserta las cartas de la tienda la primera vez que la base no tiene
--  productos. Es el mismo catalogo que siembra solo la API al arrancar
--  (PixMarketAPI/Data/InicializadorBaseDatos.cs), por si prefieres insertarlo
--  a mano desde phpMyAdmin, Workbench o la consola de MySQL.
--
--  Es idempotente por nombre: si una carta ya existe no se vuelve a insertar.
--
--  Las rutas de imagen apuntan a archivos ya subidos en
--  PixMarketAPI/wwwroot/Imagenes.
-- ============================================================================

USE `PixMarket`;


INSERT INTO `Items` (`Nombre`, `Juego`, `Categoria`, `Rareza`, `Precio`, `Stock`, `ImagenRuta`)
SELECT 'Blue-Eyes White Dragon', 'Yu-Gi-Oh!', 'Monstruo', 'Ultra Rare', 320.00, 25, '/Imagenes/item-debc8305c316497f9c329e111887879c.jpeg'
WHERE NOT EXISTS (SELECT 1 FROM `Items` WHERE `Nombre` = 'Blue-Eyes White Dragon');

INSERT INTO `Items` (`Nombre`, `Juego`, `Categoria`, `Rareza`, `Precio`, `Stock`, `ImagenRuta`)
SELECT 'Dark Magician', 'Yu-Gi-Oh!', 'Monstruo', 'Ultra Rare', 280.00, 20, '/Imagenes/item-d80d0f13ab4e4b5993e8490843ec0160.png'
WHERE NOT EXISTS (SELECT 1 FROM `Items` WHERE `Nombre` = 'Dark Magician');

INSERT INTO `Items` (`Nombre`, `Juego`, `Categoria`, `Rareza`, `Precio`, `Stock`, `ImagenRuta`)
SELECT 'Monstruo Renacido', 'Yu-Gi-Oh!', 'Trampa', 'Super Rare', 120.00, 30, '/Imagenes/item-7be6912c7a294c4cb78c8f4747d9e0af.jpeg'
WHERE NOT EXISTS (SELECT 1 FROM `Items` WHERE `Nombre` = 'Monstruo Renacido');

INSERT INTO `Items` (`Nombre`, `Juego`, `Categoria`, `Rareza`, `Precio`, `Stock`, `ImagenRuta`)
SELECT 'Ciber Dragón', 'Yu-Gi-Oh!', 'Monstruo', 'Rare', 90.00, 40, '/Imagenes/item-678e48add7f841dab2ceefb8f2f535f4.png'
WHERE NOT EXISTS (SELECT 1 FROM `Items` WHERE `Nombre` = 'Ciber Dragón');

INSERT INTO `Items` (`Nombre`, `Juego`, `Categoria`, `Rareza`, `Precio`, `Stock`, `ImagenRuta`)
SELECT 'Dragón Rojo de Ojos Oscuros', 'Yu-Gi-Oh!', 'Monstruo', 'Secret Rare', 540.00, 12, '/Imagenes/item-debc8305c316497f9c329e111887879c.jpeg'
WHERE NOT EXISTS (SELECT 1 FROM `Items` WHERE `Nombre` = 'Dragón Rojo de Ojos Oscuros');

INSERT INTO `Items` (`Nombre`, `Juego`, `Categoria`, `Rareza`, `Precio`, `Stock`, `ImagenRuta`)
SELECT 'Pikachu ex', 'Pokémon', 'Monstruo', 'Ultra Rare', 260.00, 15, '/Imagenes/item-436c7700f0924f1687f59ea4c57a844a.jpg'
WHERE NOT EXISTS (SELECT 1 FROM `Items` WHERE `Nombre` = 'Pikachu ex');

INSERT INTO `Items` (`Nombre`, `Juego`, `Categoria`, `Rareza`, `Precio`, `Stock`, `ImagenRuta`)
SELECT 'Charizard ex', 'Pokémon', 'Monstruo', 'Secret Rare', 450.00, 10, '/Imagenes/item-2fc3ca16854c4d8bb5b6721f52da7ca4.png'
WHERE NOT EXISTS (SELECT 1 FROM `Items` WHERE `Nombre` = 'Charizard ex');

INSERT INTO `Items` (`Nombre`, `Juego`, `Categoria`, `Rareza`, `Precio`, `Stock`, `ImagenRuta`)
SELECT 'Mewtwo V', 'Pokémon', 'Monstruo', 'Super Rare', 200.00, 12, '/Imagenes/item-debc8305c316497f9c329e111887879c.jpeg'
WHERE NOT EXISTS (SELECT 1 FROM `Items` WHERE `Nombre` = 'Mewtwo V');

INSERT INTO `Items` (`Nombre`, `Juego`, `Categoria`, `Rareza`, `Precio`, `Stock`, `ImagenRuta`)
SELECT 'Zapdos ex', 'Pokémon', 'Monstruo', 'Ultra Rare', 240.00, 14, '/Imagenes/item-678e48add7f841dab2ceefb8f2f535f4.png'
WHERE NOT EXISTS (SELECT 1 FROM `Items` WHERE `Nombre` = 'Zapdos ex');

INSERT INTO `Items` (`Nombre`, `Juego`, `Categoria`, `Rareza`, `Precio`, `Stock`, `ImagenRuta`)
SELECT 'Energía de Fuego', 'Pokémon', 'Entrenador', 'Common', 15.00, 80, '/Imagenes/item-d80d0f13ab4e4b5993e8490843ec0160.png'
WHERE NOT EXISTS (SELECT 1 FROM `Items` WHERE `Nombre` = 'Energía de Fuego');

INSERT INTO `Items` (`Nombre`, `Juego`, `Categoria`, `Rareza`, `Precio`, `Stock`, `ImagenRuta`)
SELECT 'Black Lotus', 'Magic: The Gathering', 'Otros', 'Rare', 950.00, 5, '/Imagenes/item-436c7700f0924f1687f59ea4c57a844a.jpg'
WHERE NOT EXISTS (SELECT 1 FROM `Items` WHERE `Nombre` = 'Black Lotus');

INSERT INTO `Items` (`Nombre`, `Juego`, `Categoria`, `Rareza`, `Precio`, `Stock`, `ImagenRuta`)
SELECT 'Ajani, Mentor de Héroes', 'Magic: The Gathering', 'Hechizo', 'Super Rare', 330.00, 8, '/Imagenes/item-2fc3ca16854c4d8bb5b6721f52da7ca4.png'
WHERE NOT EXISTS (SELECT 1 FROM `Items` WHERE `Nombre` = 'Ajani, Mentor de Héroes');

INSERT INTO `Items` (`Nombre`, `Juego`, `Categoria`, `Rareza`, `Precio`, `Stock`, `ImagenRuta`)
SELECT 'Jace, el Escultor de Mentes', 'Magic: The Gathering', 'Hechizo', 'Ultra Rare', 400.00, 6, '/Imagenes/item-d80d0f13ab4e4b5993e8490843ec0160.png'
WHERE NOT EXISTS (SELECT 1 FROM `Items` WHERE `Nombre` = 'Jace, el Escultor de Mentes');

INSERT INTO `Items` (`Nombre`, `Juego`, `Categoria`, `Rareza`, `Precio`, `Stock`, `ImagenRuta`)
SELECT 'Bola de Fuego', 'Magic: The Gathering', 'Hechizo', 'Common', 45.00, 60, '/Imagenes/item-7be6912c7a294c4cb78c8f4747d9e0af.jpeg'
WHERE NOT EXISTS (SELECT 1 FROM `Items` WHERE `Nombre` = 'Bola de Fuego');