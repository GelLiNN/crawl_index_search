<?php
	class Player
	{
		private $playerInfo;
		//indices: Name, GP, FGP, TPP, FTP, PPG
		
		//constructs a new player with given stats
		public function __construct($newInfo)
		{
			$this->playerInfo = $newInfo;
		}
		
		//returns this player's name
		public function getName() {
			return $this->playerInfo['playerName'];
		}
		
		//returns this player's info concatenated with spaces
		public function getInfo()
		{
			$s = '';
			foreach ($this->playerInfo as $token) {
				$s .= $token . '     ';
			}
			return $s;
		}
	}
?>
