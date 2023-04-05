myApp.controller("networkController",
function($scope,Factory,$rootScope){
    $rootScope.loading = true;

	let removeState = State.loading;
	let addState = State.loading;

	function updateDisplay(){
		switch(removeState){
			case State.loading: 
				$scope.removeButtonText = "Loading...";
				break;
			case State.running: 
				$scope.removeButtonText = "Stop";
				break;
			case State.stopped: 
				$scope.removeButtonText = "Start";
				break;
			default:  
				$scope.removeButtonText = "Error";
				break;
		}

		switch(addState){
			case State.loading: 
				$scope.addButtonText = "Loading...";
				break;
			case State.running: 
				$scope.addButtonText = "Stop";
				break;
			case State.stopped: 
				$scope.addButtonText = "Start";
				break;
			default:  
				$scope.addButtonText = "Error";
				break;
		}
	}

    Factory.getProxyCount().then((data)=>{
		if(data.data.retCode != 0){
			alert(data.data.retMessage);
		}
		else{
			$scope.proxyCount = data.data;
		}
	});

	let updateRemoveState = () => {
		Factory.getRemoveState().then((data)=>{

			if(data.data.retCode != 0){
				alert(data.data.retMessage);
			}
			else{
				let isRemoveActiv = data.data.result.isActiv;
			
				if(isRemoveActiv){
					removeState = State.running;
				}
				else{
					removeState = State.stopped;
				}

				updateDisplay();
			}
		});
	};
	
	let updateAddState = () => {
		Factory.getAddState().then((data)=>{

			if(data.data.retCode != 0){
				alert(data.data.retMessage);
			}
			else{
				let isAddActiv = data.data.result.isActiv;
			
				if(isAddActiv){
					addState = State.running;
				}
				else{
					addState = State.stopped;
				}

				updateDisplay();
			}
		});
	};

	//update affichage
	updateAddState();
	updateRemoveState();

	function startRemove(){

		Factory.getRemoveState().then((data)=>{
			if(data.data.retCode != 0){
				alert(data.data.retMessage);
			}
			else{
				let isRemoveActiv = data.data.result.isActiv;

				if(isRemoveActiv){
					alert("remove était déja activé");
				}
				else{
					Factory.startRemove().then((data)=>
					{
						if(data.data.retCode != 0){
							alert(data.data.retMessage);
						}
						else{
							updateRemoveState();
						}
					});
				}
			}
		});
	}

	function stopRemove(){

		Factory.getRemoveState().then((data)=>{
			if(data.data.retCode != 0){
				alert(data.data.retMessage);
			}
			else{
				let isRemoveActiv = data.data.result.isActiv;

				if(!isRemoveActiv){
					alert("remove était déja désactivé");
				}
				else{
					Factory.stopRemove().then((data)=>
					{
						if(data.data.retCode != 0){
							alert(data.data.retMessage);
						}
						else{
							updateRemoveState();
						}
					});
				}
			}
		});
	}

	$scope.removeButtonClickEventHandler = function(){
		if(removeState == State.running){
			removeState = State.loading;
			updateDisplay();
			stopRemove();
		}
		else{
			removeState = State.loading;
			updateDisplay();
			startRemove();
		}
	};

	function startAdd(){

		Factory.getAddState().then((data)=>{
			if(data.data.retCode != 0){
				alert(data.data.retMessage);
			}
			else{
				let isAddActiv = data.data.result.isActiv;

				if(isAddActiv){
					alert("add était déja acitvé");
				}
				else{
					Factory.startAdd().then((data)=>
					{
						if(data.data.retCode != 0){
							alert(data.data.retMessage);
						}
						else{
							updateAddState();
						}
					});
				}
			}
		});
	}

	function stopAdd(){

		Factory.getAddState().then((data)=>{
			if(data.data.retCode != 0){
				alert(data.data.retMessage);
			}
			else{
				let isAddActiv = data.data.result.isActiv;

				if(!isAddActiv){
					alert("add était déja désactivé");
				}
				else{
					Factory.stopAdd().then((data)=>
					{
						if(data.data.retCode != 0){
							alert(data.data.retMessage);
						}
						else{
							updateAddState();
							getRemovePourcentage();
						}
					});
				}
			}
		});
	}

	$scope.addButtonClickEventHandler = function(){
		if(addState == State.running){
			addState = State.loading;
			updateDisplay();
			stopAdd();
		}
		else{
			addState = State.loading;
			updateDisplay();
			startAdd();
		}
	};

	//$scope.displayAddInformationsButton = addState == State.running;
	//$scope.displayRemoveInformationsButton = removeState == State.running;

	function getRemovePourcentage(){
		Factory.getRemovePourcentage().then((data)=>{
			if(data.data.retCode != 0){
				alert(data.data.retMessage);
			}
			else{
				$scope.removePourcentage = data.data.result.pourcentage;
			}
		});
	} 

	getRemovePourcentage();

	$rootScope.loading = false;
}
);