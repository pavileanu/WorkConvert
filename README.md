# PhpSmallProject

1. Instal xampp ( it comes with php, mysql, apache )
2. 
    htttpd conf changes:
    
    Listen 8000
    ServerName localhost:8000
    
    httpd ssl conf changes:
    
    Listen 8001
    <VirtualHost _default_:8001>
    ServerName localhost:8001
    
3. Configure Site root:

    DocumentRoot "C:/Site"
    <Directory "C:/Site">

4. Configure php in cmd:

  Control Panel > System Security > System > Advance System Settings > Advance > Environments Variables >  
  Path Variable ( put ; (php file location))

5. Basic php site 
