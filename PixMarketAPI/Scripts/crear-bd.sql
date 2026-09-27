-- ============================================================================
--  PixMarket: creacion completa de la base de datos
-- ----------------------------------------------------------------------------
--  Este archivo se GENERO desde el modelo de EF Core con:
--      dotnet run --project PixMarketAPI -- --volcar-esquema
--  Volvelo a generar cada vez que cambies una entidad, para que no quede viejo.
--
--  OJO: esto solo sirve para crear la base desde cero (phpMyAdmin, un servidor
--  nuevo, la instalacion de un companero). En el dia a dia no hace falta:
--  la API ya crea y actualiza sola la base al arrancar
--  (PixMarketAPI/Data/InicializadorBaseDatos.cs).
--
--  Para crear o actualizar la base de datos en tu equipo:
--      .\Scripts\actualizar-bd.ps1
--
--  Este script NO inserta datos. La API inserta sola el administrador
--  (admin@pixmarket.com / admin123, ya cifrado con PBKDF2) y la configuracion
--  de la tienda cuando la base queda vacia.
-- ============================================================================

CREATE DATABASE IF NOT EXISTS `PixMarket`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_0900_ai_ci;

USE `PixMarket`;

ALTER DATABASE CHARACTER SET utf8mb4;


CREATE TABLE `Configuraciones` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `NombreTienda` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `CorreoContacto` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Telefono` varchar(30) CHARACTER SET utf8mb4 NULL,
    `Moneda` varchar(10) CHARACTER SET utf8mb4 NULL,
    `Direccion` varchar(200) CHARACTER SET utf8mb4 NULL,
    `Horario` varchar(200) CHARACTER SET utf8mb4 NULL,
    `NotificarStockBajo` tinyint(1) NOT NULL,
    `AvisosNuevosPedidos` tinyint(1) NOT NULL,
    `TiendaPausada` tinyint(1) NOT NULL,
    CONSTRAINT `PK_Configuraciones` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `Items` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Nombre` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Descripcion` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Juego` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Categoria` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Rareza` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Precio` decimal(65,30) NOT NULL,
    `Stock` int NOT NULL,
    `ImagenRuta` varchar(300) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_Items` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `Usuarios` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Nombre` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Correo` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Contrasenia` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Rol` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
    `Telefono` varchar(15) CHARACTER SET utf8mb4 NOT NULL,
    `Estado` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `FechaRegistro` datetime(6) NOT NULL,
    CONSTRAINT `PK_Usuarios` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `Ventas` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `FechaVenta` datetime(6) NOT NULL,
    `Total` decimal(65,30) NOT NULL,
    `MetodoPago` varchar(100) CHARACTER SET utf8mb4 NULL,
    `IdUsuario` int NULL,
    `Estado` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
    `FechaActualizacion` datetime(6) NULL,
    CONSTRAINT `PK_Ventas` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Ventas_Usuarios_IdUsuario` FOREIGN KEY (`IdUsuario`) REFERENCES `Usuarios` (`Id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `DetallesVenta` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `IdVenta` int NOT NULL,
    `IdItem` int NOT NULL,
    `Cantidad` int NOT NULL,
    `PrecioUnitario` decimal(65,30) NOT NULL,
    `Subtotal` decimal(65,30) NOT NULL,
    CONSTRAINT `PK_DetallesVenta` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_DetallesVenta_Items_IdItem` FOREIGN KEY (`IdItem`) REFERENCES `Items` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_DetallesVenta_Ventas_IdVenta` FOREIGN KEY (`IdVenta`) REFERENCES `Ventas` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;


CREATE INDEX `IX_DetallesVenta_IdItem` ON `DetallesVenta` (`IdItem`);


CREATE INDEX `IX_DetallesVenta_IdVenta` ON `DetallesVenta` (`IdVenta`);


CREATE INDEX `IX_Ventas_IdUsuario` ON `Ventas` (`IdUsuario`);

