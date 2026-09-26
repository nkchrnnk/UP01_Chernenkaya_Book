-- ===================================================================
-- Учебная практика УП.01.01
-- Студентка: Черненькая Вероника Владимировна
-- Группа: 4ИСИП-523
-- Преподаватель: Аксёнова Т.Г.
-- Тема: Проектирование и разработка базы данных для сервиса бронирования гостиниц и апартаментов
-- База данных: DB_Chernenkaya_Booking
-- ===================================================================

USE master;
GO

IF DB_ID('DB_Chernenkaya_Booking') IS NOT NULL
BEGIN
    ALTER DATABASE DB_Chernenkaya_Booking SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE DB_Chernenkaya_Booking;
END
GO

CREATE DATABASE DB_Chernenkaya_Booking COLLATE Cyrillic_General_CI_AS;
GO

USE DB_Chernenkaya_Booking;
GO

-- 1. Таблица городов (City)
CREATE TABLE [City] (
    [CityID] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [CityName] nvarchar(100) NOT NULL
);
GO

-- 2. Таблица типов размещения (AccommodationType)
CREATE TABLE [AccommodationType] (
    [TypeID] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [TypeName] nvarchar(100) NOT NULL
);
GO

-- 3. Таблица объектов размещения (Accommodation)
CREATE TABLE [Accommodation] (
    [AccommodationID] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] nvarchar(150) NOT NULL,
    [TypeID] int NOT NULL,
    [CityID] int NOT NULL,
    [Address] nvarchar(250) NOT NULL,
    [Rooms] int NOT NULL,
    [Capacity] int NOT NULL,
    [PricePerNight] decimal(10,2) NOT NULL,
    [Rating] decimal(2,1) NULL,
    [Description] nvarchar(1000) NULL,
    [Photo] nvarchar(255) NULL,
    CONSTRAINT FK_Accommodation_Type FOREIGN KEY ([TypeID]) REFERENCES [AccommodationType] ([TypeID]) ON DELETE CASCADE,
    CONSTRAINT FK_Accommodation_City FOREIGN KEY ([CityID]) REFERENCES [City] ([CityID]) ON DELETE CASCADE
);
GO

-- 4. Таблица номеров (Room)
CREATE TABLE [Room] (
    [RoomID] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [AccommodationID] int NOT NULL,
    [RoomNumber] nvarchar(20) NOT NULL,
    [Floor] int NOT NULL,
    [RoomType] nvarchar(50) NOT NULL,
    [Capacity] int NOT NULL,
    [PricePerNight] decimal(10,2) NOT NULL,
    [IsAvailable] bit NOT NULL DEFAULT 1,
    CONSTRAINT FK_Room_Accommodation FOREIGN KEY ([AccommodationID]) REFERENCES [Accommodation] ([AccommodationID]) ON DELETE CASCADE
);
GO

-- 5. Таблица пользователей (Users)
CREATE TABLE [Users] (
    [UserID] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Login] nvarchar(50) NOT NULL UNIQUE,
    [Password] nvarchar(64) NOT NULL,
    [Role] nvarchar(20) NOT NULL,
    [FIO] nvarchar(150) NOT NULL,
    [Phone] nvarchar(30) NULL,
    [Email] nvarchar(100) NULL
);
GO

-- 6. Таблица статусов бронирования (BookingStatus)
CREATE TABLE [BookingStatus] (
    [StatusID] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [StatusName] nvarchar(50) NOT NULL
);
GO

-- 7. Таблица бронирований (Booking)
CREATE TABLE [Booking] (
    [BookingID] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserID] int NOT NULL,
    [RoomID] int NOT NULL,
    [StatusID] int NOT NULL,
    [CheckIn] date NOT NULL,
    [CheckOut] date NOT NULL,
    [Guests] int NOT NULL,
    [CreatedAt] datetime2(7) NOT NULL DEFAULT SYSDATETIME(),
    [Comment] nvarchar(500) NULL,
    CONSTRAINT FK_Booking_Users FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID]) ON DELETE CASCADE,
    CONSTRAINT FK_Booking_Room FOREIGN KEY ([RoomID]) REFERENCES [Room] ([RoomID]) ON DELETE NO ACTION,
    CONSTRAINT FK_Booking_Status FOREIGN KEY ([StatusID]) REFERENCES [BookingStatus] ([StatusID]) ON DELETE NO ACTION
);
GO

-- 8. Таблица дополнительных услуг (Service)
CREATE TABLE [Service] (
    [ServiceID] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [ServiceName] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [Price] decimal(10,2) NOT NULL
);
GO

-- 9. Таблица услуг в бронировании (BookingService)
CREATE TABLE [BookingService] (
    [BookingID] int NOT NULL,
    [ServiceID] int NOT NULL,
    [Quantity] int NOT NULL DEFAULT 1,
    [PriceAtBooking] decimal(10,2) NOT NULL,
    CONSTRAINT PK_BookingService PRIMARY KEY ([BookingID], [ServiceID]),
    CONSTRAINT FK_BookingService_Booking FOREIGN KEY ([BookingID]) REFERENCES [Booking] ([BookingID]) ON DELETE CASCADE,
    CONSTRAINT FK_BookingService_Service FOREIGN KEY ([ServiceID]) REFERENCES [Service] ([ServiceID]) ON DELETE CASCADE
);
GO

-- 10. Таблица платежей (Payment)
CREATE TABLE [Payment] (
    [PaymentID] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [BookingID] int NOT NULL,
    [PaymentDate] datetime2(7) NOT NULL DEFAULT SYSDATETIME(),
    [Amount] decimal(12,2) NOT NULL,
    [PaymentMethod] nvarchar(50) NOT NULL,
    [PaymentStatus] nvarchar(50) NOT NULL,
    CONSTRAINT FK_Payment_Booking FOREIGN KEY ([BookingID]) REFERENCES [Booking] ([BookingID]) ON DELETE CASCADE
);
GO

-- ===================================================================
-- НАПОЛНЕНИЕ НАЧАЛЬНЫМИ ТЕСТОВЫМИ ДАННЫМИ
-- Пароль SHA-1:
-- "admin123" -> F865B53623B121FD34EE5426C792E5C33AF8C227
-- "user123"  -> 38762CF7F55934B34D179AE6A4C80CADCCBB7F0A
-- ===================================================================

-- 1. Города
INSERT INTO [City] ([CityName]) VALUES
(N'Москва'),
(N'Санкт-Петербург'),
(N'Сочи'),
(N'Казань'),
(N'Калининград'),
(N'Нижний Новгород');
GO

-- 2. Типы размещения
INSERT INTO [AccommodationType] ([TypeName]) VALUES
(N'Отель'),
(N'Апартаменты'),
(N'Бутик-отель'),
(N'Загородный клуб'),
(N'Курортный комплекс');
GO

-- 3. Пользователи
INSERT INTO [Users] ([Login], [Password], [Role], [FIO], [Phone], [Email]) VALUES
(N'admin', N'F865B53623B121FD34EE5426C792E5C33AF8C227', N'Admin', N'Черненькая Вероника Владимировна', N'+7 (916) 111-22-33', N'chernenkaya@booking.fa.ru'),
(N'vanya', N'F877A712F726E55265D599FEF77209BFE8D116AB', N'User', N'Иванов Иван Сергеевич', N'+7 (999) 777-88-99', N'vanya@yandex.ru'),
(N'smirnov', N'38762CF7F55934B34D179AE6A4C80CADCCBB7F0A', N'User', N'Смирнов Дмитрий Алексеевич', N'+7 (926) 222-33-44', N'smirnov@mail.ru'),
(N'petrova', N'38762CF7F55934B34D179AE6A4C80CADCCBB7F0A', N'User', N'Петрова Елена Сергеевна', N'+7 (915) 333-44-55', N'petrova@yandex.ru'),
(N'volkov', N'38762CF7F55934B34D179AE6A4C80CADCCBB7F0A', N'User', N'Волков Максим Андреевич', N'+7 (903) 444-55-66', N'volkov@bk.ru'),
(N'morozova', N'38762CF7F55934B34D179AE6A4C80CADCCBB7F0A', N'User', N'Морозова Ксения Игоревна', N'+7 (999) 555-66-77', N'morozova@gmail.com');
GO

-- 4. Статусы бронирования
INSERT INTO [BookingStatus] ([StatusName]) VALUES
(N'Подтверждено'),
(N'Ожидает оплаты'),
(N'Завершено'),
(N'Отменено'),
(N'На рассмотрении');
GO

-- 5. Дополнительные сервисы
INSERT INTO [Service] ([ServiceName], [Description], [Price]) VALUES
(N'Завтрак "Шведский стол"', N'Ежедневный горячий завтрак со свежей выпечкой и напитками', 850.00),
(N'Трансфер из аэропорта', N'Встреча с табличкой и комфортный седан премиум-класса', 2400.00),
(N'Посещение SPA и бассейна', N'Доступ в термальный комплекс, финскую сауну и хаммам на день', 1600.00),
(N'Подземная охраняемая парковка', N'Фиксированное машиноместо с круглосуточным видеонаблюдением', 600.00),
(N'Поздний выезд до 18:00', N'Возможность освободить номер во второй половине дня', 1900.00);
GO

-- 6. Объекты размещения (Accommodation)
INSERT INTO [Accommodation] ([Name], [TypeID], [CityID], [Address], [Rooms], [Capacity], [PricePerNight], [Rating], [Description], [Photo]) VALUES
(N'Гранд Отель Националь', 1, 1, N'г. Москва, ул. Моховая, д. 15', 40, 90, 7500.00, 4.9, N'Исторический пятизвездочный отель в самом сердце столицы с панорамным видом на Кремль.', N'/res/hotel1.png'),
(N'Северная Пальмира Апартс', 2, 2, N'г. Санкт-Петербург, наб. реки Мойки, д. 42', 25, 55, 5200.00, 4.8, N'Дизайнерские просторные апартаменты в классическом петербургском стиле рядом с Эрмитажем.', N'/res/hotel2.png'),
(N'Черноморская Ривьера Резорт', 5, 3, N'г. Сочи, Курортный проспект, д. 88', 60, 150, 8900.00, 4.9, N'Курортный комплекс на первой береговой линии с собственным оборудованным пляжем и парком.', N'/res/hotel3.png'),
(N'Казанский Дворик Бутик-Отель', 3, 4, N'г. Казань, ул. Баумана, д. 28', 18, 36, 4600.00, 4.7, N'Уютный бутик-отель на пешеходной исторической улице с авторским татарским рестораном.', N'/res/hotel4.png'),
(N'Балтийский Бриз Парк-Отель', 4, 5, N'г. Калининград, ул. Верхнеозерная, д. 19', 30, 70, 4900.00, 4.6, N'Загородный клуб на живописном берегу озера с прогулочной зоной и спа-центром.', N'/res/hotel5.png'),
(N'Волжский Откос Премиум', 1, 6, N'г. Нижний Новгород, Верхневолжская наб., д. 7', 35, 75, 5100.00, 4.7, N'Современный отель с лучшей смотровой панорамой на слияние Волги и Оки.', N'/res/hotel6.png'),
(N'Столичные Апартаменты на Тверской', 2, 1, N'г. Москва, ул. Тверская, д. 9', 15, 30, 6800.00, 4.8, N'Стильные видовые апартаменты с оборудованной кухней в 5 минутах от Красной площади.', N'/res/hotel1.png'),
(N'Адмиралтейский Бутик-Отель', 3, 2, N'г. Санкт-Петербург, ул. Большая Морская, д. 35', 20, 40, 5800.00, 4.8, N'Камерный отель в историческом особняке с высокими сводчатыми потолками.', N'/res/hotel2.png');
GO

-- 7. Номера (Room)
INSERT INTO [Room] ([AccommodationID], [RoomNumber], [Floor], [RoomType], [Capacity], [PricePerNight], [IsAvailable]) VALUES
(1, N'101', 1, N'Стандарт Double', 2, 7500.00, 1),
(1, N'205', 2, N'Делюкс с видом на Кремль', 2, 9800.00, 1),
(1, N'310', 3, N'Президентский Люкс', 4, 18500.00, 0),
(2, N'A-1', 2, N'Студия с террасой', 2, 5200.00, 1),
(2, N'A-4', 3, N'Двухуровневые апартаменты', 4, 7900.00, 1),
(3, N'301', 3, N'Морской Люкс с балконом', 2, 8900.00, 0),
(3, N'412', 4, N'Семейный Семейный номер', 4, 12400.00, 1),
(4, N'12', 2, N'Улучшенный Стандарт', 2, 4600.00, 1),
(4, N'24', 3, N'Люкс Ханский', 3, 7200.00, 1),
(5, N'102', 1, N'Коттедж у озера', 4, 4900.00, 1),
(6, N'204', 2, N'Панорамный Стандарт', 2, 5100.00, 1),
(7, N'ТВ-5', 4, N'Бизнес-Апартаменты', 2, 6800.00, 1),
(8, N'21', 2, N'Арт-Люкс Адмирал', 2, 5800.00, 0);
GO

-- 8. Бронирования (Booking)
INSERT INTO [Booking] ([UserID], [RoomID], [StatusID], [CheckIn], [CheckOut], [Guests], [CreatedAt], [Comment]) VALUES
(2, 1, 3, '2026-09-10', '2026-09-14', 2, '2026-09-01T12:00:00', N'Просьба предоставить тихий номер.'),
(2, 6, 1, '2026-10-18', '2026-10-24', 2, '2026-10-02T14:30:00', N'Празднование годовщины, требуется приветственный комплимент.'),
(3, 4, 3, '2026-09-20', '2026-09-25', 2, '2026-09-15T10:15:00', N'Заезд после 20:00.'),
(3, 2, 1, '2026-10-20', '2026-10-23', 2, '2026-10-05T16:20:00', N'Необходимы раздельные кровати.'),
(4, 8, 3, '2026-09-12', '2026-09-15', 2, '2026-09-08T09:40:00', N'Деловая командировка.'),
(4, 3, 2, '2026-10-25', '2026-10-29', 3, '2026-10-09T18:00:00', N'Ожидается подтверждение корпоративного счета.'),
(5, 10, 1, '2026-10-16', '2026-10-19', 4, '2026-10-04T11:25:00', N'Отдых с семьей.'),
(5, 13, 1, '2026-11-01', '2026-11-05', 2, '2026-10-10T15:50:00', N'Ранний заезд в 09:00.'),
(2, 5, 4, '2026-09-28', '2026-09-30', 2, '2026-09-22T13:10:00', N'Отменено клиентом в связи с изменением планов.'),
(3, 7, 1, '2026-11-10', '2026-11-16', 4, '2026-10-12T17:40:00', N'Детская кроватка в номер.');
GO

-- 9. Услуги в бронированиях (BookingService)
INSERT INTO [BookingService] ([BookingID], [ServiceID], [Quantity], [PriceAtBooking]) VALUES
(1, 1, 4, 850.00),
(1, 2, 1, 2400.00),
(2, 1, 6, 850.00),
(2, 3, 2, 1600.00),
(3, 1, 5, 850.00),
(4, 1, 3, 850.00),
(4, 4, 3, 600.00),
(5, 1, 3, 850.00),
(6, 2, 2, 2400.00),
(7, 3, 4, 1600.00),
(8, 5, 1, 1900.00),
(10, 1, 6, 850.00);
GO

-- 10. Платежи (Payment)
INSERT INTO [Payment] ([BookingID], [PaymentDate], [Amount], [PaymentMethod], [PaymentStatus]) VALUES
(1, '2026-09-01T12:05:00', 35800.00, N'Банковская карта', N'Оплачено'),
(2, '2026-10-02T14:40:00', 61700.00, N'СБП', N'Оплачено'),
(3, '2026-09-15T10:20:00', 30250.00, N'Банковская карта', N'Оплачено'),
(4, '2026-10-05T16:25:00', 33750.00, N'Банковская карта', N'Оплачено'),
(5, '2026-09-08T09:45:00', 16350.00, N'СБП', N'Оплачено'),
(7, '2026-10-04T11:30:00', 21100.00, N'Банковская карта', N'Оплачено'),
(8, '2026-10-10T16:00:00', 25100.00, N'Наличные при заезде', N'В обработке'),
(10, '2026-10-12T17:45:00', 79500.00, N'СБП', N'Оплачено');
GO

PRINT N'База данных DB_Chernenkaya_Booking успешно создана и заполнена тестовыми данными!';
GO
