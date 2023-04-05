var myApp = angular.module("myApp",["ngRoute"]);

myApp.config(
function($routeProvider){
	$routeProvider
		.when('/network',{templateUrl: 'partials/network.html',controller: 'networkController'})
		.when('/trader',{templateUrl: 'partials/trader.html',controller: 'traderController'})
		.when('/addTrader',{templateUrl: 'partials/addTrader.html',controller: 'addTraderController'})
		.when('/socketInformation/:socketEndpoint',{templateUrl: 'partials/socketInformation.html',controller: 'socketInformationController'})
		.otherwise({redirectTo : '/trader'})
});

