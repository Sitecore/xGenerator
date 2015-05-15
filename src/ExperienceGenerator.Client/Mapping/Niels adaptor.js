function adapt(doc) {
	var request = {
		VisitorCount: 1*doc.Overview.Visits.NumberOfUniqueVisitors
	};
	
	var defaultSegment = {
		VisitCount: 1*doc.Overview.Visits.NumberOfVisitsGenerated/doc.Overview.Visits.NumberOfUniqueVisitors,
		PageViews: 1*doc.Overview.Visits.PageviewsPerVisitAvg,
		Identified: 1*doc.Overview.Visits.PercentageIdentifiedVisitors/100,
		Duration: durationToSeconds(doc.Overview.Visits.TimeSpentPerVisitAvg),
		StartDate: new Date(doc.Overview.Dates.StartDate).toISOString(),
		EndDate: new Date(doc.Overview.Dates.EndDate).toISOString(),
		YearlyTrend: 1 + (doc.Overview.Dates.AnnualIncreaseInTraffic / 100),		
		DayOfWeek: toWeights(doc.Overview.DailyDistribution),
		Month: toWeights(doc.Overview.MonthlyDistribution, function(value, i){return i + 1;}),
		LandingPage: {
			Site: toWeights(doc.Overview.TrafficDistribution),
			Item:  toWeights(doc.LandingPages)
		},
		Channel: toWeights(doc.Channels),
		Referrer: toWeights(doc.RefURLs),	
		InternalSearch: {
			Percentage: doc.Search.PercentageTrafficFromSearch.Percentage/100,
			Keywords: toWeights(doc.Search.InternalSearchTerms)
		},
		ExternalSearch: {
			Percentage: doc.Search.PercentageTrafficFromSearch.Percentage/100,
			Keywords: toWeights(doc.Search.ExternalSearchTerms),
			Engine: joinDicts(toWeights(doc.Search.Organic), toWeights(doc.Search.PPC))
		},
		
		Geo: {
			Region: toWeights(doc.Overview.Location)
		}
	}
	
	request.Specification = {
		Segments: {
			Default: defaultSegment
		}
	}
	return request;
}

function joinDicts(d1, d2) {	
	for( var key in d2 ) {
		d1[key] = d2[key];
	}
	return d1;
}

function toWeights(o, keyTranslator) {
	var weights = {};
	var i = 0;
	keyTranslator = keyTranslator || function(value, i) { return value; }
	for(var key in o ) {		
		weights[keyTranslator(key, i++)] = 1*o[key];
	}
	return weights;
}

function durationToSeconds(s) {
	var parts = s.split(":");
	var i = 0;
	var duration = 0;
	if( parts.length > 2 ) duration += parts[i++] * 3600;
	if( parts.length > 1 ) duration += parts[i++] * 60;
	return duration + 1*parts[i];
}