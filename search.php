<?php
//require database tier and player object
require_once("Data.php");
require_once("Player.php");
$conn = makePDO();

//logic tier, only display results if user already entered a query!
$alreadyqueried = false;
if (isset($_GET["playername"])) {
	$alreadyqueried = true;
	$query = $_GET["playername"];
} ?>

<!DOCTYPE html>
<html>
	<head>
		<title>NBA Search!</title>
		<link href="style.css" type="text/css" rel="stylesheet" />
	</head>
	<body>
		<h1>Enter Player Name:</h1>
		<form action="Search.php" method="get">
			<input name="playername" type="text" size="20" placeholder="Name" autofocus="autofocus" />
		</form>
		
		<!--Sees if we need to print results-->
		<?php
			if ($alreadyqueried) { ?>
				<!--If the user already submitted a query, we know to print-->
				<ul><h2>STATS RETRIEVED :</h2><br />
				<span>NAME</span>
				<span>Field Goal Percentage</span>
				<span>Three Point Percentage</span>
				<span>Free Throw Percentage</span>
				<span>Points Per Game</span>
				<?php printPlayers($query, $conn); ?>
				</ul>
		<?php } ?>
	</body>
</html>
