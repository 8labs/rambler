interface String {
    startsWith(searchString: string, position?: number | undefined): boolean;
}

if (!String.prototype.startsWith) {
	String.prototype.startsWith = function(search: string, pos: number) {
		return this.substr(!pos || pos < 0 ? 0 : +pos, search.length) === search;
	};
}