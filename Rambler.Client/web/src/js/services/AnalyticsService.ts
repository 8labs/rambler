namespace Rambler {

    export class AnalyticsService {

        static $inject = [];
        constructor() {
        }

        event(event: string, label: string, value: string) {
            if (!Config.environment.production) { return; }
            gtag('event', event, {
                'event_category': 'ChatEvent',
                'event_label': label,
                'event_value': value
            });
        }

    }
}

angular
    .module('rambler')
    .service('AnalyticsService', Rambler.AnalyticsService);
