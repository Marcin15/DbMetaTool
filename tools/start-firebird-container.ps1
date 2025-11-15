docker run -d `
	--name firebird-db `
	--env-file ".env" `
	-p 3050:3050 `
	-v ./data:/var/lib/firebird/data `
	 firebirdsql/firebird:5