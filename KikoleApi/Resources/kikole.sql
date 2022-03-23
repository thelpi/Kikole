SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET time_zone = "+00:00";

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

CREATE DATABASE IF NOT EXISTS `kikole` DEFAULT CHARACTER SET utf8 COLLATE utf8_bin;
USE `kikole`;

CREATE TABLE `badges` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `name` varchar(255) COLLATE utf8_bin NOT NULL,
  `description` text COLLATE utf8_bin NOT NULL,
  `users` bigint(20) UNSIGNED NOT NULL DEFAULT '0',
  `hidden` tinyint(3) UNSIGNED NOT NULL DEFAULT '0',
  `creation_date` datetime NOT NULL,
  `update_date` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `clubs` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `name` varchar(255) COLLATE utf8_bin NOT NULL,
  `allowed_names` text COLLATE utf8_bin NOT NULL,
  `creation_date` datetime NOT NULL,
  `update_date` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `countries` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `code` char(2) COLLATE utf8_bin NOT NULL,
  `creation_date` datetime NOT NULL,
  `update_date` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

INSERT INTO `countries` (`id`, `code`, `creation_date`, `update_date`) VALUES
(1, 'AF', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(2, 'AX', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(3, 'AL', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(4, 'DZ', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(5, 'AS', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(6, 'AD', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(7, 'AO', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(8, 'AI', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(9, 'AQ', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(10, 'AG', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(11, 'AR', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(12, 'AM', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(13, 'AW', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(14, 'AU', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(15, 'AT', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(16, 'AZ', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(17, 'BS', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(18, 'BH', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(19, 'BD', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(20, 'BB', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(21, 'BY', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(22, 'BE', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(23, 'BZ', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(24, 'BJ', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(25, 'BM', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(26, 'BT', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(27, 'BO', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(28, 'BQ', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(29, 'BA', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(30, 'BW', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(31, 'BV', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(32, 'BR', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(33, 'IO', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(34, 'BN', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(35, 'BG', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(36, 'BF', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(37, 'BI', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(38, 'CV', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(39, 'KH', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(40, 'CM', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(41, 'CA', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(42, 'KY', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(43, 'CF', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(44, 'TD', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(45, 'CL', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(46, 'CN', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(47, 'CX', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(48, 'CC', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(49, 'CO', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(50, 'KM', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(51, 'CG', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(52, 'CD', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(53, 'CK', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(54, 'CR', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(55, 'CI', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(56, 'HR', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(57, 'CU', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(58, 'CW', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(59, 'CY', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(60, 'CZ', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(61, 'DK', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(62, 'DJ', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(63, 'DM', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(64, 'DO', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(65, 'EC', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(66, 'EG', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(67, 'SV', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(68, 'GQ', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(69, 'ER', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(70, 'EE', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(71, 'SZ', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(72, 'ET', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(73, 'FK', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(74, 'FO', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(75, 'FJ', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(76, 'FI', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(77, 'FR', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(78, 'GF', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(79, 'PF', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(80, 'TF', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(81, 'GA', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(82, 'GM', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(83, 'GE', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(84, 'DE', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(85, 'GH', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(86, 'GI', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(87, 'GR', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(88, 'GL', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(89, 'GD', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(90, 'GP', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(91, 'GU', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(92, 'GT', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(93, 'GG', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(94, 'GN', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(95, 'GW', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(96, 'GY', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(97, 'HT', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(98, 'HM', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(99, 'VA', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(100, 'HN', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(101, 'HK', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(102, 'HU', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(103, 'IS', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(104, 'IN', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(105, 'ID', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(106, 'IR', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(107, 'IQ', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(108, 'IE', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(109, 'IM', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(110, 'IL', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(111, 'IT', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(112, 'JM', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(113, 'JP', '2022-03-03 22:17:41', '2022-03-03 21:17:41'),
(114, 'JE', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(115, 'JO', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(116, 'KZ', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(117, 'KE', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(118, 'KI', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(119, 'KP', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(120, 'KR', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(121, 'KW', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(122, 'KG', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(123, 'LA', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(124, 'LV', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(125, 'LB', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(126, 'LS', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(127, 'LR', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(128, 'LY', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(129, 'LI', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(130, 'LT', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(131, 'LU', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(132, 'MO', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(133, 'MG', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(134, 'MW', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(135, 'MY', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(136, 'MV', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(137, 'ML', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(138, 'MT', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(139, 'MH', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(140, 'MQ', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(141, 'MR', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(142, 'MU', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(143, 'YT', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(144, 'MX', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(145, 'FM', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(146, 'MD', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(147, 'MC', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(148, 'MN', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(149, 'ME', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(150, 'MS', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(151, 'MA', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(152, 'MZ', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(153, 'MM', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(154, 'NA', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(155, 'NR', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(156, 'NP', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(157, 'NL', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(158, 'NC', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(159, 'NZ', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(160, 'NI', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(161, 'NE', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(162, 'NG', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(163, 'NU', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(164, 'NF', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(165, 'MK', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(166, 'MP', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(167, 'NO', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(168, 'OM', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(169, 'PK', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(170, 'PW', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(171, 'PS', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(172, 'PA', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(173, 'PG', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(174, 'PY', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(175, 'PE', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(176, 'PH', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(177, 'PN', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(178, 'PL', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(179, 'PT', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(180, 'PR', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(181, 'QA', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(182, 'RE', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(183, 'RO', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(184, 'RU', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(185, 'RW', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(186, 'BL', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(187, 'SH', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(188, 'KN', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(189, 'LC', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(190, 'MF', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(191, 'PM', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(192, 'VC', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(193, 'WS', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(194, 'SM', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(195, 'ST', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(196, 'SA', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(197, 'SN', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(198, 'RS', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(199, 'SC', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(200, 'SL', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(201, 'SG', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(202, 'SX', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(203, 'SK', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(204, 'SI', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(205, 'SB', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(206, 'SO', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(207, 'ZA', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(208, 'GS', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(209, 'SS', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(210, 'ES', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(211, 'LK', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(212, 'SD', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(213, 'SR', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(214, 'SJ', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(215, 'SE', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(216, 'CH', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(217, 'SY', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(218, 'TW', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(219, 'TJ', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(220, 'TZ', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(221, 'TH', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(222, 'TL', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(223, 'TG', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(224, 'TK', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(225, 'TO', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(226, 'TT', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(227, 'TN', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(228, 'TR', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(229, 'TM', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(230, 'TC', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(231, 'TV', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(232, 'UG', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(233, 'UA', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(234, 'AE', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(235, 'GB', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(236, 'US', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(237, 'UM', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(238, 'UY', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(239, 'UZ', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(240, 'VU', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(241, 'VE', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(242, 'VN', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(243, 'VG', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(244, 'VI', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(245, 'WF', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(246, 'EH', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(247, 'YE', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(248, 'ZM', '2022-03-03 22:17:42', '2022-03-03 21:17:42'),
(249, 'ZW', '2022-03-03 22:17:42', '2022-03-03 21:17:42');

CREATE TABLE `country_translations` (
  `country_id` bigint(20) UNSIGNED NOT NULL,
  `language_id` bigint(20) UNSIGNED NOT NULL,
  `name` varchar(255) COLLATE utf8_bin NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

INSERT INTO `country_translations` (`country_id`, `language_id`, `name`) VALUES
(1, 1, 'Afghanistan'),
(2, 1, 'Åland Islands'),
(3, 1, 'Albania'),
(4, 1, 'Algeria'),
(5, 1, 'American Samoa'),
(6, 1, 'Andorra'),
(7, 1, 'Angola'),
(8, 1, 'Anguilla'),
(9, 1, 'Antarctica'),
(10, 1, 'Antigua and Barbuda'),
(11, 1, 'Argentina'),
(12, 1, 'Armenia'),
(13, 1, 'Aruba'),
(14, 1, 'Australia'),
(15, 1, 'Austria'),
(16, 1, 'Azerbaijan'),
(17, 1, 'Bahamas'),
(18, 1, 'Bahrain'),
(19, 1, 'Bangladesh'),
(20, 1, 'Barbados'),
(21, 1, 'Belarus'),
(22, 1, 'Belgium'),
(23, 1, 'Belize'),
(24, 1, 'Benin'),
(25, 1, 'Bermuda'),
(26, 1, 'Bhutan'),
(27, 1, 'Bolivia (Plurinational State of)'),
(28, 1, 'Bonaire, Sint Eustatius and Saba'),
(29, 1, 'Bosnia and Herzegovina'),
(30, 1, 'Botswana'),
(31, 1, 'Bouvet Island'),
(32, 1, 'Brazil'),
(33, 1, 'British Indian Ocean Territory'),
(34, 1, 'Brunei Darussalam'),
(35, 1, 'Bulgaria'),
(36, 1, 'Burkina Faso'),
(37, 1, 'Burundi'),
(38, 1, 'Cabo Verde'),
(39, 1, 'Cambodia'),
(40, 1, 'Cameroon'),
(41, 1, 'Canada'),
(42, 1, 'Cayman Islands'),
(43, 1, 'Central African Republic'),
(44, 1, 'Chad'),
(45, 1, 'Chile'),
(46, 1, 'China'),
(47, 1, 'Christmas Island'),
(48, 1, 'Cocos (Keeling) Islands'),
(49, 1, 'Colombia'),
(50, 1, 'Comoros'),
(51, 1, 'Congo'),
(52, 1, 'Congo, Democratic Republic of the'),
(53, 1, 'Cook Islands'),
(54, 1, 'Costa Rica'),
(55, 1, 'Côte d\'Ivoire'),
(56, 1, 'Croatia'),
(57, 1, 'Cuba'),
(58, 1, 'Curaçao'),
(59, 1, 'Cyprus'),
(60, 1, 'Czechia'),
(61, 1, 'Denmark'),
(62, 1, 'Djibouti'),
(63, 1, 'Dominica'),
(64, 1, 'Dominican Republic'),
(65, 1, 'Ecuador'),
(66, 1, 'Egypt'),
(67, 1, 'El Salvador'),
(68, 1, 'Equatorial Guinea'),
(69, 1, 'Eritrea'),
(70, 1, 'Estonia'),
(71, 1, 'Eswatini'),
(72, 1, 'Ethiopia'),
(73, 1, 'Falkland Islands (Malvinas)'),
(74, 1, 'Faroe Islands'),
(75, 1, 'Fiji'),
(76, 1, 'Finland'),
(77, 1, 'France'),
(78, 1, 'French Guiana'),
(79, 1, 'French Polynesia'),
(80, 1, 'French Southern Territories'),
(81, 1, 'Gabon'),
(82, 1, 'Gambia'),
(83, 1, 'Georgia'),
(84, 1, 'Germany'),
(85, 1, 'Ghana'),
(86, 1, 'Gibraltar'),
(87, 1, 'Greece'),
(88, 1, 'Greenland'),
(89, 1, 'Grenada'),
(90, 1, 'Guadeloupe'),
(91, 1, 'Guam'),
(92, 1, 'Guatemala'),
(93, 1, 'Guernsey'),
(94, 1, 'Guinea'),
(95, 1, 'Guinea-Bissau'),
(96, 1, 'Guyana'),
(97, 1, 'Haiti'),
(98, 1, 'Heard Island and McDonald Islands'),
(99, 1, 'Holy See'),
(100, 1, 'Honduras'),
(101, 1, 'Hong Kong'),
(102, 1, 'Hungary'),
(103, 1, 'Iceland'),
(104, 1, 'India'),
(105, 1, 'Indonesia'),
(106, 1, 'Iran (Islamic Republic of)'),
(107, 1, 'Iraq'),
(108, 1, 'Ireland'),
(109, 1, 'Isle of Man'),
(110, 1, 'Israel'),
(111, 1, 'Italy'),
(112, 1, 'Jamaica'),
(113, 1, 'Japan'),
(114, 1, 'Jersey'),
(115, 1, 'Jordan'),
(116, 1, 'Kazakhstan'),
(117, 1, 'Kenya'),
(118, 1, 'Kiribati'),
(119, 1, 'Korea (Democratic People\'s Republic of)'),
(120, 1, 'Korea, Republic of'),
(121, 1, 'Kuwait'),
(122, 1, 'Kyrgyzstan'),
(123, 1, 'Lao People\'s Democratic Republic'),
(124, 1, 'Latvia'),
(125, 1, 'Lebanon'),
(126, 1, 'Lesotho'),
(127, 1, 'Liberia'),
(128, 1, 'Libya'),
(129, 1, 'Liechtenstein'),
(130, 1, 'Lithuania'),
(131, 1, 'Luxembourg'),
(132, 1, 'Macao'),
(133, 1, 'Madagascar'),
(134, 1, 'Malawi'),
(135, 1, 'Malaysia'),
(136, 1, 'Maldives'),
(137, 1, 'Mali'),
(138, 1, 'Malta'),
(139, 1, 'Marshall Islands'),
(140, 1, 'Martinique'),
(141, 1, 'Mauritania'),
(142, 1, 'Mauritius'),
(143, 1, 'Mayotte'),
(144, 1, 'Mexico'),
(145, 1, 'Micronesia (Federated States of)'),
(146, 1, 'Moldova, Republic of'),
(147, 1, 'Monaco'),
(148, 1, 'Mongolia'),
(149, 1, 'Montenegro'),
(150, 1, 'Montserrat'),
(151, 1, 'Morocco'),
(152, 1, 'Mozambique'),
(153, 1, 'Myanmar'),
(154, 1, 'Namibia'),
(155, 1, 'Nauru'),
(156, 1, 'Nepal'),
(157, 1, 'Netherlands'),
(158, 1, 'New Caledonia'),
(159, 1, 'New Zealand'),
(160, 1, 'Nicaragua'),
(161, 1, 'Niger'),
(162, 1, 'Nigeria'),
(163, 1, 'Niue'),
(164, 1, 'Norfolk Island'),
(165, 1, 'North Macedonia'),
(166, 1, 'Northern Mariana Islands'),
(167, 1, 'Norway'),
(168, 1, 'Oman'),
(169, 1, 'Pakistan'),
(170, 1, 'Palau'),
(171, 1, 'Palestine, State of'),
(172, 1, 'Panama'),
(173, 1, 'Papua New Guinea'),
(174, 1, 'Paraguay'),
(175, 1, 'Peru'),
(176, 1, 'Philippines'),
(177, 1, 'Pitcairn'),
(178, 1, 'Poland'),
(179, 1, 'Portugal'),
(180, 1, 'Puerto Rico'),
(181, 1, 'Qatar'),
(182, 1, 'Réunion'),
(183, 1, 'Romania'),
(184, 1, 'Russian Federation'),
(185, 1, 'Rwanda'),
(186, 1, 'Saint Barthélemy'),
(187, 1, 'Saint Helena, Ascension and Tristan da Cunha'),
(188, 1, 'Saint Kitts and Nevis'),
(189, 1, 'Saint Lucia'),
(190, 1, 'Saint Martin (French part)'),
(191, 1, 'Saint Pierre and Miquelon'),
(192, 1, 'Saint Vincent and the Grenadines'),
(193, 1, 'Samoa'),
(194, 1, 'San Marino'),
(195, 1, 'Sao Tome and Principe'),
(196, 1, 'Saudi Arabia'),
(197, 1, 'Senegal'),
(198, 1, 'Serbia'),
(199, 1, 'Seychelles'),
(200, 1, 'Sierra Leone'),
(201, 1, 'Singapore'),
(202, 1, 'Sint Maarten (Dutch part)'),
(203, 1, 'Slovakia'),
(204, 1, 'Slovenia'),
(205, 1, 'Solomon Islands'),
(206, 1, 'Somalia'),
(207, 1, 'South Africa'),
(208, 1, 'South Georgia and the South Sandwich Islands'),
(209, 1, 'South Sudan'),
(210, 1, 'Spain'),
(211, 1, 'Sri Lanka'),
(212, 1, 'Sudan'),
(213, 1, 'Suriname'),
(214, 1, 'Svalbard and Jan Mayen'),
(215, 1, 'Sweden'),
(216, 1, 'Switzerland'),
(217, 1, 'Syrian Arab Republic'),
(218, 1, 'Taiwan, Province of China'),
(219, 1, 'Tajikistan'),
(220, 1, 'Tanzania, United Republic of'),
(221, 1, 'Thailand'),
(222, 1, 'Timor-Leste'),
(223, 1, 'Togo'),
(224, 1, 'Tokelau'),
(225, 1, 'Tonga'),
(226, 1, 'Trinidad and Tobago'),
(227, 1, 'Tunisia'),
(228, 1, 'Turkey'),
(229, 1, 'Turkmenistan'),
(230, 1, 'Turks and Caicos Islands'),
(231, 1, 'Tuvalu'),
(232, 1, 'Uganda'),
(233, 1, 'Ukraine'),
(234, 1, 'United Arab Emirates'),
(235, 1, 'United Kingdom of Great Britain and Northern Ireland'),
(236, 1, 'United States of America'),
(237, 1, 'United States Minor Outlying Islands'),
(238, 1, 'Uruguay'),
(239, 1, 'Uzbekistan'),
(240, 1, 'Vanuatu'),
(241, 1, 'Venezuela (Bolivarian Republic of)'),
(242, 1, 'Viet Nam'),
(243, 1, 'Virgin Islands (British)'),
(244, 1, 'Virgin Islands (U.S.)'),
(245, 1, 'Wallis and Futuna'),
(246, 1, 'Western Sahara'),
(247, 1, 'Yemen'),
(248, 1, 'Zambia'),
(249, 1, 'Zimbabwe');

CREATE TABLE `languages` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `code` char(2) COLLATE utf8_bin NOT NULL,
  `name` varchar(255) COLLATE utf8_bin NOT NULL,
  `creation_date` datetime NOT NULL,
  `update_date` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

INSERT INTO `languages` (`id`, `code`, `name`, `creation_date`, `update_date`) VALUES
(1, 'en', 'English', '2022-03-03 00:00:00', '2022-03-03 21:03:16'),
(2, 'fr', 'Français', '2022-03-03 00:00:00', '2022-03-03 21:03:16');

CREATE TABLE `leaders` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `user_id` bigint(20) UNSIGNED NOT NULL,
  `proposal_date` date NOT NULL,
  `points` smallint(5) UNSIGNED NOT NULL,
  `time` smallint(5) UNSIGNED NOT NULL,
  `creation_date` datetime NOT NULL,
  `update_date` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `messages` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `message` text COLLATE utf8_bin NOT NULL,
  `display_from` datetime DEFAULT NULL,
  `display_to` datetime DEFAULT NULL,
  `creation_date` datetime NOT NULL,
  `update_date` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `players` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `name` varchar(255) COLLATE utf8_bin NOT NULL,
  `allowed_names` text COLLATE utf8_bin NOT NULL,
  `year_of_birth` smallint(5) UNSIGNED NOT NULL,
  `country_id` bigint(20) UNSIGNED NOT NULL,
  `proposal_date` date DEFAULT NULL,
  `clue` varchar(255) COLLATE utf8_bin NOT NULL,
  `position_id` bigint(20) UNSIGNED NOT NULL,
  `badge_id` bigint(20) UNSIGNED DEFAULT NULL,
  `creation_user_id` bigint(20) UNSIGNED NOT NULL,
  `creation_date` datetime NOT NULL,
  `update_date` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `reject_date` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `player_clubs` (
  `player_id` bigint(20) UNSIGNED NOT NULL,
  `club_id` bigint(20) UNSIGNED NOT NULL,
  `history_position` tinyint(3) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `positions` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `name` varchar(255) COLLATE utf8_bin NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

INSERT INTO `positions` (`id`, `name`) VALUES
(1, 'Goalkeeper'),
(2, 'Defender'),
(3, 'Midfielder'),
(4, 'Forward');

CREATE TABLE `proposals` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `user_id` bigint(20) UNSIGNED NOT NULL,
  `proposal_type_id` bigint(20) UNSIGNED NOT NULL,
  `value` varchar(255) COLLATE utf8_bin DEFAULT NULL,
  `successful` tinyint(3) UNSIGNED NOT NULL,
  `proposal_date` datetime NOT NULL,
  `days_before` int(10) UNSIGNED NOT NULL DEFAULT '0',
  `creation_date` datetime NOT NULL,
  `update_date` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `proposal_types` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `name` varchar(255) COLLATE utf8_bin NOT NULL,
  `description` text COLLATE utf8_bin NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

INSERT INTO `proposal_types` (`id`, `name`, `description`) VALUES
(1, 'Name', 'The player\'s name has been proposed'),
(2, 'Club', 'A club in the player\'s career has been proposed'),
(3, 'Year', 'The player\'s year of the birth has been proposed'),
(4, 'Country', 'The player\'s nationality has been proposed'),
(6, 'Position', 'The player\'s position has been proposed');

CREATE TABLE `users` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `login` varchar(255) COLLATE utf8_bin NOT NULL,
  `password` char(64) COLLATE utf8_bin NOT NULL,
  `password_reset_question` varchar(255) COLLATE utf8_bin NOT NULL,
  `password_reset_answer` char(64) COLLATE utf8_bin NOT NULL,
  `language_id` bigint(20) UNSIGNED NOT NULL,
  `user_type_id` bigint(20) UNSIGNED NOT NULL,
  `is_disabled` tinyint(3) UNSIGNED NOT NULL DEFAULT '0',
  `creation_date` datetime NOT NULL,
  `update_date` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `user_badges` (
  `user_id` bigint(20) UNSIGNED NOT NULL,
  `badge_id` bigint(20) UNSIGNED NOT NULL,
  `get_date` date NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `user_types` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `name` varchar(255) COLLATE utf8_bin NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

INSERT INTO `user_types` (`id`, `name`) VALUES
(1, 'Standard user'),
(2, 'Power user'),
(3, 'Administrator');


ALTER TABLE `badges`
  ADD PRIMARY KEY (`id`);

ALTER TABLE `clubs`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `name` (`name`);

ALTER TABLE `countries`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `code` (`code`);

ALTER TABLE `country_translations`
  ADD PRIMARY KEY (`country_id`,`language_id`),
  ADD KEY `country_id` (`country_id`),
  ADD KEY `language_id` (`language_id`);

ALTER TABLE `languages`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `code` (`code`);

ALTER TABLE `leaders`
  ADD PRIMARY KEY (`id`),
  ADD KEY `user_id` (`user_id`),
  ADD KEY `proposal_date` (`proposal_date`);

ALTER TABLE `messages`
  ADD PRIMARY KEY (`id`);

ALTER TABLE `players`
  ADD PRIMARY KEY (`id`),
  ADD KEY `country_id` (`country_id`) USING BTREE,
  ADD KEY `position_id` (`position_id`),
  ADD KEY `badge_id` (`badge_id`),
  ADD KEY `creation_user_id` (`creation_user_id`),
  ADD KEY `reject_date` (`reject_date`);

ALTER TABLE `player_clubs`
  ADD PRIMARY KEY (`player_id`,`club_id`) USING BTREE,
  ADD UNIQUE KEY `player_id_2` (`player_id`,`history_position`),
  ADD KEY `player_id` (`player_id`),
  ADD KEY `club_id` (`club_id`);

ALTER TABLE `positions`
  ADD PRIMARY KEY (`id`);

ALTER TABLE `proposals`
  ADD PRIMARY KEY (`id`),
  ADD KEY `user_id` (`user_id`),
  ADD KEY `proposal_type_id` (`proposal_type_id`);

ALTER TABLE `proposal_types`
  ADD PRIMARY KEY (`id`);

ALTER TABLE `users`
  ADD PRIMARY KEY (`id`),
  ADD KEY `lang_id` (`language_id`),
  ADD KEY `is_disabled` (`is_disabled`),
  ADD KEY `user_type_id` (`user_type_id`);

ALTER TABLE `user_badges`
  ADD PRIMARY KEY (`user_id`,`badge_id`);

ALTER TABLE `user_types`
  ADD PRIMARY KEY (`id`);


ALTER TABLE `badges`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE `clubs`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE `countries`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE `languages`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE `leaders`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE `messages`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE `players`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE `positions`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE `proposals`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE `proposal_types`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE `users`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE `user_types`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
