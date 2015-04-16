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
	
	//append to paragraph element
	echo '<p>';
	//integer count fixes double name printing
	$count = 0;
	foreach ($results as $result) {
		//print each players stats
		foreach ($result as $stat) {
			if ($count > 0) {
				//include spacing for aesthetics
				echo $stat . '&nbsp&nbsp&nbsp&nbsp&nbsp';
			}
			$count++;
		}
		count = 0;
		echo '<hr /><br />';
	}
	echo '</p>';
}

?>
