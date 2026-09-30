-- ============================================================================
--  PixMarket: cartas provisionales (fotos WhatsApp)
--  Nombres genericos "Carta R1/A1/...": editalos despues desde el panel.
-- ============================================================================

USE `PixMarket`;

INSERT INTO Items (Nombre, Juego, Categoria, Rareza, Precio, Stock, ImagenRuta) SELECT 'Carta R1', 'Otros', 'Otros', 'Common', 50.00, 10, '/Imagenes/item-5d609b0170434696ad45a2b22815524a.jpeg' WHERE NOT EXISTS (SELECT 1 FROM Items WHERE Nombre = 'Carta R1');
INSERT INTO Items (Nombre, Juego, Categoria, Rareza, Precio, Stock, ImagenRuta) SELECT 'Carta R2', 'Otros', 'Otros', 'Common', 50.00, 10, '/Imagenes/item-3fa8755104db4607bee79d0880360ad1.jpeg' WHERE NOT EXISTS (SELECT 1 FROM Items WHERE Nombre = 'Carta R2');
INSERT INTO Items (Nombre, Juego, Categoria, Rareza, Precio, Stock, ImagenRuta) SELECT 'Carta A1', 'Otros', 'Otros', 'Common', 50.00, 10, '/Imagenes/item-5bdee5ff2ab34834bf9728278dce7789.jpeg' WHERE NOT EXISTS (SELECT 1 FROM Items WHERE Nombre = 'Carta A1');
INSERT INTO Items (Nombre, Juego, Categoria, Rareza, Precio, Stock, ImagenRuta) SELECT 'Carta A2', 'Otros', 'Otros', 'Common', 50.00, 10, '/Imagenes/item-d36a770a96b24406b381515f5f1d923a.jpeg' WHERE NOT EXISTS (SELECT 1 FROM Items WHERE Nombre = 'Carta A2');
INSERT INTO Items (Nombre, Juego, Categoria, Rareza, Precio, Stock, ImagenRuta) SELECT 'Carta A3', 'Otros', 'Otros', 'Common', 50.00, 10, '/Imagenes/item-2a16938510dd491a9b6b259697797c55.jpeg' WHERE NOT EXISTS (SELECT 1 FROM Items WHERE Nombre = 'Carta A3');
INSERT INTO Items (Nombre, Juego, Categoria, Rareza, Precio, Stock, ImagenRuta) SELECT 'Carta A4', 'Otros', 'Otros', 'Common', 50.00, 10, '/Imagenes/item-a7c9237bf06644a7b3380cea18e9e33f.jpeg' WHERE NOT EXISTS (SELECT 1 FROM Items WHERE Nombre = 'Carta A4');
INSERT INTO Items (Nombre, Juego, Categoria, Rareza, Precio, Stock, ImagenRuta) SELECT 'Carta A5', 'Otros', 'Otros', 'Common', 50.00, 10, '/Imagenes/item-c87c3677271a43ed8ae6f8bdf1940620.jpeg' WHERE NOT EXISTS (SELECT 1 FROM Items WHERE Nombre = 'Carta A5');
INSERT INTO Items (Nombre, Juego, Categoria, Rareza, Precio, Stock, ImagenRuta) SELECT 'Carta A6', 'Otros', 'Otros', 'Common', 50.00, 10, '/Imagenes/item-4510afe1be02406890605e67a8dad077.jpeg' WHERE NOT EXISTS (SELECT 1 FROM Items WHERE Nombre = 'Carta A6');
INSERT INTO Items (Nombre, Juego, Categoria, Rareza, Precio, Stock, ImagenRuta) SELECT 'Carta A7', 'Otros', 'Otros', 'Common', 50.00, 10, '/Imagenes/item-4a76f0861a614861a7458588c3588219.jpeg' WHERE NOT EXISTS (SELECT 1 FROM Items WHERE Nombre = 'Carta A7');
INSERT INTO Items (Nombre, Juego, Categoria, Rareza, Precio, Stock, ImagenRuta) SELECT 'Carta B1', 'Otros', 'Otros', 'Common', 50.00, 10, '/Imagenes/item-86ce74c00f244a84ac8e6f5ccf55dd4d.jpeg' WHERE NOT EXISTS (SELECT 1 FROM Items WHERE Nombre = 'Carta B1');
INSERT INTO Items (Nombre, Juego, Categoria, Rareza, Precio, Stock, ImagenRuta) SELECT 'Carta B2', 'Otros', 'Otros', 'Common', 50.00, 10, '/Imagenes/item-2073c1c3a1e84c6d8d7f27b8b7c180f0.jpeg' WHERE NOT EXISTS (SELECT 1 FROM Items WHERE Nombre = 'Carta B2');
INSERT INTO Items (Nombre, Juego, Categoria, Rareza, Precio, Stock, ImagenRuta) SELECT 'Carta B3', 'Otros', 'Otros', 'Common', 50.00, 10, '/Imagenes/item-2a2761bc8b8d4dc2beffe04feca86593.jpeg' WHERE NOT EXISTS (SELECT 1 FROM Items WHERE Nombre = 'Carta B3');
INSERT INTO Items (Nombre, Juego, Categoria, Rareza, Precio, Stock, ImagenRuta) SELECT 'Carta B4', 'Otros', 'Otros', 'Common', 50.00, 10, '/Imagenes/item-8d33a906db6b41e88028522f8549f50a.jpeg' WHERE NOT EXISTS (SELECT 1 FROM Items WHERE Nombre = 'Carta B4');
INSERT INTO Items (Nombre, Juego, Categoria, Rareza, Precio, Stock, ImagenRuta) SELECT 'Carta B5', 'Otros', 'Otros', 'Common', 50.00, 10, '/Imagenes/item-e2bfd1524b9544c9807a271f70ff9ef7.jpeg' WHERE NOT EXISTS (SELECT 1 FROM Items WHERE Nombre = 'Carta B5');
INSERT INTO Items (Nombre, Juego, Categoria, Rareza, Precio, Stock, ImagenRuta) SELECT 'Carta B6', 'Otros', 'Otros', 'Common', 50.00, 10, '/Imagenes/item-3c6b034cb14d41c2b68c9f21683638e8.jpeg' WHERE NOT EXISTS (SELECT 1 FROM Items WHERE Nombre = 'Carta B6');
