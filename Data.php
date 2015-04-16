<?php
//Database tier for 3-tier architecture

//returns the PDO object that represents connection to SQL database
function makePDO() {
	try {
		$conn = new PDO('mysql:host=pa1db.c9invte2jx6b.us-west-2.rds.amazonaws.com;dbname=pa1db',
			'info344user',
			'<password>');
		$conn->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
				
		return $conn;
	} catch(PDOException $e) {
		echo 'ERROR: ' . $e->getMessage();
	}
}

//queries the given database with the given name, and prints results
function printPlayers($name, $conn) {
	$stmt = $conn->prepare("SELECT playername, GP, FGP, TPP, FTP, PPG FROM players
										WHERE playername LIKE '%$name%'");
	$stmt->execute();
	$results = $stmt->fetchAll();
	
	//append to list as list element
	echo '<li>';
	//boolean toPrint fixes double name printing
	$toPrint = false;
	foreach ($results as $result) {
		//print each players stats
		foreach ($result as $stat) {
			if ($toPrint) {
				//include spacing for aesthetics
				echo $stat . '&nbsp&nbsp&nbsp-&nbsp&nbsp&nbsp';
				$toPrint = false;
			} else {
				$toPrint = true;
			}
		}
		echo '<hr />';
	}
	echo '</li>';
}

?>
