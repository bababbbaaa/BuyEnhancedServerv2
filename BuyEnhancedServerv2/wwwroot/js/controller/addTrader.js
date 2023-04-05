myApp.controller("addTraderController",
function($scope,Factory,$rootScope){
    $rootScope.loading = true;

    let areInformationsValid = State.undifined;
    let addTraderButtonState = State.hidden;

    function updateDisplay(){
        if(areInformationsValid == State.valid && $scope.pourcentage != undefined){
            addTraderButtonState = State.shown;
        }

        switch(areInformationsValid){
            case State.undifined: 
				$scope.verifyInformationsButtonText = "Vérifier les informations";
				break;
            case State.loading: 
				$scope.verifyInformationsButtonText = "Loading...";
				break;
			case State.valid: 
				$scope.verifyInformationsButtonText = "Valid";
				break;
			case State.invalid: 
				$scope.verifyInformationsButtonText = "Invalid";
				break;
			default:  
				$scope.verifyInformationsButtonText = "Error";
				break;
        }

        switch(addTraderButtonState){
            case State.hidden: 
				$scope.addTraderButtonValue = "_______";
				break;
            case State.loading: 
				$scope.addTraderButtonValue = "Loading...";
				break;
			case State.shown: 
				$scope.addTraderButtonValue = "Ajouter";
				break;
			default:  
				$scope.addTraderButtonValue = "Error";
				break;
        }
    }

    $scope.clearFields = function (){
        areInformationsValid = State.undifined;

        $scope.encryptedUid = "";
        $scope.apiKey = "";
        $scope.apiSecret = "";
        $scope.pourcentage = undefined;
        addTraderButtonState = State.hidden;

        updateDisplay();
    }

    $scope.onChangeEvantHandler = function (){
        areInformationsValid = State.undifined;
        updateDisplay();
    }

    $scope.eventHandlerClickVerifyInformations = function(){
        switch(areInformationsValid){
            case State.undifined:
                areInformationsValid = State.loading;
                updateDisplay();
                Factory.areValidInformations($scope.encryptedUid,$scope.apiKey,$scope.apiSecret).then(
                    (data)=>{
                        if(data.retCode!=0){
                            alert(data.retMessage);
                        }
                        else{
                            if(data.result.isValid){
                                areInformationsValid = State.valid;
                            }
                            else{
                                areInformationsValid = State.invalid;
                            }
                        }
                        updateDisplay();
                    }
                );
				break;
			default:  
				break;
        }
    }

    $scope.addTrader = function (){
        if(areInformationsValid == State.valid){
            addTraderButton = State.loading;
            updateDisplay();
            Factory.addTrader($scope.encryptedUid,$scope.apiKey,$scope.apiSecret,$scope.pourcentage).then(
                (data)=>{
                    if(data.retCode == 0){
                        $scope.clearFields();
                    }
                    else{
                        alert(data.retMessage);
                    }
                }
            );
        }
    }

    $scope.updateDisplayScope = updateDisplay;

    updateDisplay();

	$rootScope.loading = false;
}
);