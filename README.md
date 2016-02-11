# GOOGLE infrastructure .net cloud application
The Google infrastructure is made up of three modules that can be thought of separately.  These are the crawling, indexing, and searching.

# Crawling :
This is done in this .net cloud application by scraping valid links from XML sitemaps and raw Html recursively using libraries from Nuget, assessing links for validity and storing those in Azure's data structures (Cloud Table and Cloud Queue).  In a month, over 20 million web pages were crawled by this project.

# Indexing :
This is done by adding the scraped URLS to an Azure cloud table, to be retrieved later for storage in a Trie data structure.  URLS, page titles, and other metadata must be stored in the cloud table in order for the project to really simulate a webpage indexing engine like Google's.

# Searching :
This is done by retrieving patterns of Web Page titles extremely efficiently from the Trie data structure (Trie.cs).  The retrieval is so quick, that every time you type a letter, 10 matching web pages pop up asynchronously as query suggestions.  All of this programming helps to create the familiar Google experience.
