class Factory{
	constructor($http, $q){
		this.http = $http;
		this.q = $q;
		this.origin = "http://172.20.4.118:5218";
	}

	async postAPI(origin,endpoint,data) {
		let deffered = this.q.defer();
		fetch(origin+endpoint,{
			method : "POST",
			mode : "cors",
			cache: "no-cache",
			credentials: "same-origin",
			redirect: "follow",
			referrerPolicy: "no-referrer",
			body: JSON.stringify(data)
		}).then((data)=>{deffered.resolve(data.json());},()=>{deffered.reject("Impossible")});

		return deffered.promise;
	}

	getProxyCount(){
		let deffered = this.q.defer();

		this.http.get(this.origin+"/proxyCount").then(
			(data)=>{deffered.resolve(data);},
			()=>{deffered.reject("Impossible");}
		);
			
		return deffered.promise;
	}

	getRemoveState(){
		let deffered = this.q.defer();
		this.http.get(this.origin+"/isRemoveActiv").then(
			(data)=>{deffered.resolve(data);},
			()=>{deffered.reject("Impossible");}
		);

		return deffered.promise;
	}

	getRemovePourcentage(){
		let deffered = this.q.defer();

		this.http.get(this.origin+"/getRemovePourcentage").then(
			(data)=>{deffered.resolve(data);},
			()=>{deffered.reject("Impossible");}
		);
			
		return deffered.promise;
	}

	getAddState(){
		let deffered = this.q.defer();
		this.http.get(this.origin+"/isAddActiv").then(
			(data)=>{deffered.resolve(data);},
			()=>{deffered.reject("Impossible");}
		);

		return deffered.promise;
	}

	startRemove(){
		let deffered = this.q.defer();
		this.http.get(this.origin+"/startRemove").then(
			(data)=>{deffered.resolve(data);},
			()=>{deffered.reject("Impossible");}
		);

		return deffered.promise;
	}

	stopRemove(){
		let deffered = this.q.defer();
		this.http.get(this.origin+"/stopRemove").then(
			(data)=>{deffered.resolve(data);},
			()=>{deffered.reject("Impossible");}
		);

		return deffered.promise;
	}

	startAdd(){
		let deffered = this.q.defer();
		this.http.get(this.origin+"/startAdd").then(
		(data)=>{deffered.resolve(data);},
		()=>{deffered.reject("Impossible");}
		);

		return deffered.promise;
	}

	stopAdd(){
		let deffered = this.q.defer();
		this.http.get(this.origin+"/stopAdd").then(
			(data)=>{deffered.resolve(data);},
			()=>{deffered.reject("Impossible");}
		);

		return deffered.promise;
	}

	areValidInformations(anEncryptedUid,anApiKey,anApiSecret){
		let data = {
			'anApiKey' : anApiKey,
			'anApiSecret' : anApiSecret,
			'anEncryptedUid' : anEncryptedUid
		}

		let deffered = this.q.defer();

		this.postAPI(this.origin,"/areValidInformations", data).then(
			(data)=>{deffered.resolve(data);},
			()=>{deffered.reject("Impossible");}
		);
		
		return deffered.promise;
	}

	addTrader(anEncryptedUid,anApiKey,anApiSecret,aPourcentage){
		let data = {
			'anApiKey' : anApiKey,
			'anApiSecret' : anApiSecret,
			'anEncryptedUid' : anEncryptedUid,
			'aPourcentage' : aPourcentage
		}

		let deffered = this.q.defer();

		this.postAPI(this.origin,"/addTrader", data).then(
			(data)=>{deffered.resolve(data);},
			()=>{deffered.reject("Impossible");}
		);
		
		return deffered.promise;
	}

	getSubscriptionList(){
		let deffered = this.q.defer();
		this.http.get(this.origin+"/getSubscriptionList").then(
			(data)=>{deffered.resolve(data);},
			()=>{deffered.reject("Impossible");}
		);

		return deffered.promise;
	}

	launchSubscription(encryptedUid){
		let data = {
			'anEncryptedUid' : encryptedUid
		}

		let deffered = this.q.defer();

		this.postAPI(this.origin,"/launchSubscription", data).then(
			(data)=>{deffered.resolve(data);},
			()=>{deffered.reject("Impossible");}
		);
		
		return deffered.promise;
	}

	softStop(encryptedUid){
		let data = {
			'anEncryptedUid' : encryptedUid,
		}

		let deffered = this.q.defer();

		this.postAPI(this.origin,"/softStop", data).then(
			(data)=>{deffered.resolve(data);},
			()=>{deffered.reject("Impossible");}
		);
		
		return deffered.promise;
	}

	brutalStop(encryptedUid){
		let data = {
			'anEncryptedUid' : encryptedUid,
		}

		let deffered = this.q.defer();

		this.postAPI(this.origin,"/brutalStop", data).then(
			(data)=>{deffered.resolve(data);},
			()=>{deffered.reject("Impossible");}
		);
		
		return deffered.promise;
	}

	deleteTrader(encryptedUid){
		let data = {
			'anEncryptedUid' : encryptedUid,
		}

		let deffered = this.q.defer();

		this.postAPI(this.origin,"/deleteTrader", data).then(
			(data)=>{deffered.resolve(data);},
			()=>{deffered.reject("Impossible");}
		);
		
		return deffered.promise;
	}

	openTraderSocket(encryptedUid){
		let data = {
			'anEncryptedUid' : encryptedUid,
		}

		let deffered = this.q.defer();

		this.postAPI(this.origin,"/openTraderSocket", data).then(
			(data)=>{deffered.resolve(data);},
			()=>{deffered.reject("Impossible");}
		);
		
		return deffered.promise;
	}

	closeTraderSocket(encryptedUid){
		let deffered = this.q.defer();
		this.http.get(this.origin+"/closeTraderSocket").then(
			(data)=>{deffered.resolve(data);},
			()=>{deffered.reject("Impossible");}
		);

		return deffered.promise;
	}
}

myApp.factory('Factory',Factory);

